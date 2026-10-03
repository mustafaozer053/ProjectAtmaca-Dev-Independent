using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Organizations.Assignments;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Organizations;

[ApiController]
[Route("api/organizations/{organizationId:guid}/duty-assignments")]
public sealed class OrganizationDutyAssignmentsController(
    ListOrganizationDutyAssignmentsQueryHandler listHandler,
    AddOrganizationDutyAssignmentCommandHandler addHandler,
    EndOrganizationDutyAssignmentCommandHandler endHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationDutyAssignmentResponse>>> List(
        Guid organizationId,
        [FromQuery] Guid atmacaCardId,
        CancellationToken cancellationToken)
    {
        var result = await listHandler.Handle(organizationId, atmacaCardId, cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationDutyAssignmentResponse>> Add(
        Guid organizationId,
        [FromBody] AddOrganizationDutyAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await addHandler.Handle(
            new AddOrganizationDutyAssignmentCommand(
                organizationId,
                request.AtmacaCardId,
                request.Title ?? string.Empty,
                request.StartDate,
                request.EndDate),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Created(
            $"/api/organizations/{organizationId:D}/duty-assignments?atmacaCardId={request.AtmacaCardId:D}",
            ToResponse(result.Value!));
    }

    [HttpPost("{assignmentId:guid}/end")]
    public async Task<IActionResult> End(
        Guid organizationId,
        Guid assignmentId,
        [FromBody] EndOrganizationDutyAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await endHandler.Handle(
            new EndOrganizationDutyAssignmentCommand(
                organizationId,
                request.AtmacaCardId,
                assignmentId,
                request.EndDate),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        int status = error.Code switch
        {
            "ORGANIZATION_NOT_FOUND" or "ATMACA_CARD_NOT_FOUND" or
                "ORGANIZATION_DUTY_ASSIGNMENT_NOT_FOUND" => 404,
            "ORGANIZATION_DUTY_DUPLICATE" or "ORGANIZATION_DUTY_ALREADY_ENDED" or
                "ORGANIZATION_INACTIVE" => 409,
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

    private static OrganizationDutyAssignmentResponse ToResponse(
        OrganizationDutyAssignmentDetails details) =>
        new(
            details.Id,
            details.AtmacaCardId,
            details.OrganizationId,
            details.Title,
            details.StartDate,
            details.EndDate);
}

public sealed record AddOrganizationDutyAssignmentRequest(
    Guid AtmacaCardId,
    string? Title,
    DateTime StartDate,
    DateTime? EndDate);

public sealed record EndOrganizationDutyAssignmentRequest(
    Guid AtmacaCardId,
    DateTime EndDate);

public sealed record OrganizationDutyAssignmentResponse(
    Guid Id,
    Guid AtmacaCardId,
    Guid OrganizationId,
    string Title,
    DateTime StartDate,
    DateTime? EndDate);
