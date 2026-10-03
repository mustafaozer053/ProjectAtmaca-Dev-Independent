using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.ManageProfessionalTitles;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Persons;

[ApiController]
[Route("api/persons/{personId:guid}/professional-titles")]
public sealed class PersonProfessionalTitlesController(
    GetProfessionalTitlesQueryHandler getHandler,
    AddProfessionalTitleCommandHandler addHandler,
    EndProfessionalTitleCommandHandler endHandler,
    UpdateProfessionalTitleCommandHandler updateHandler,
    RemoveProfessionalTitleCommandHandler removeHandler,
    LinkProfessionalTitleDocumentCommandHandler linkDocumentHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProfessionalTitleResponse>>> Get(
        Guid personId,
        CancellationToken cancellationToken)
    {
        var result = await getHandler.Handle(personId, cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ProfessionalTitleResponse>> Add(
        Guid personId,
        [FromBody] AddProfessionalTitleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await addHandler.Handle(
            new AddProfessionalTitleCommand(personId, request.Title ?? string.Empty, request.StartedOn),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        var response = ToResponse(result.Value!);
        return CreatedAtAction(nameof(Get), new { personId }, response);
    }

    [HttpPost("{titleId:guid}/end")]
    public async Task<IActionResult> End(
        Guid personId,
        Guid titleId,
        [FromBody] EndProfessionalTitleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await endHandler.Handle(
            new EndProfessionalTitleCommand(personId, titleId, request.EndedOn),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    [HttpPut("{titleId:guid}")]
    public async Task<IActionResult> Update(
        Guid personId,
        Guid titleId,
        [FromBody] UpdateProfessionalTitleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateHandler.Handle(
            new UpdateProfessionalTitleCommand(personId, titleId, request.Title ?? string.Empty, request.StartedOn),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    [HttpDelete("{titleId:guid}")]
    public async Task<IActionResult> Remove(
        Guid personId,
        Guid titleId,
        CancellationToken cancellationToken)
    {
        var result = await removeHandler.Handle(
            new RemoveProfessionalTitleCommand(personId, titleId),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    [HttpPost("{titleId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> LinkDocument(
        Guid personId,
        Guid titleId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await linkDocumentHandler.Handle(
            new LinkProfessionalTitleDocumentCommand(personId, titleId, documentId),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        int status = error.Code switch
        {
            "PERSON_NOT_FOUND" or "PERSON_PROFESSIONAL_TITLE_NOT_FOUND" or
                "ATMACA_CARD_NOT_FOUND" or "ATMACA_CARD_DOCUMENT_NOT_FOUND" => 404,
            "PERSON_PROFESSIONAL_TITLE_DUPLICATE" or "PERSON_PROFESSIONAL_TITLE_ALREADY_ENDED" or
                "PERSON_PROFESSIONAL_TITLE_DOCUMENT_DUPLICATE" or
                "PERSON_PROFESSIONAL_TITLE_HAS_EVIDENCE" => 409,
            var code when code == ActorAuthorizationErrors.Forbidden.Code => 403,
            _ => 400
        };
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = error.Message
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        var response = StatusCode(status, problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }

    private static ProfessionalTitleResponse ToResponse(ProfessionalTitleDetails details) =>
        new(details.Id, details.Title, details.StartedOn, details.EndedOn, details.EvidenceDocumentIds);
}

public sealed record AddProfessionalTitleRequest(string? Title, DateOnly StartedOn);
public sealed record UpdateProfessionalTitleRequest(string? Title, DateOnly StartedOn);
public sealed record EndProfessionalTitleRequest(DateOnly EndedOn);
public sealed record ProfessionalTitleResponse(
    Guid Id,
    string Title,
    DateOnly StartedOn,
    DateOnly? EndedOn,
    IReadOnlyList<Guid> EvidenceDocumentIds);
