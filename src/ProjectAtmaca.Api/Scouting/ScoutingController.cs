using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Scouting;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;

namespace ProjectAtmaca.Api.Scouting;

public sealed record ChangeScoutingDecisionRequest(ScoutingDecision Decision);

public sealed record LinkScoutingRegistrationRequest(Guid PersonId);

[ApiController]
[Route("api/scouting/candidates")]
public sealed class ScoutingController(ScoutingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScoutingCandidateListItem>>> List(
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : Ok(result.Value!);
    }

    [HttpGet("{candidateId:guid}")]
    public async Task<ActionResult<ScoutingCandidateDetails>> GetDetails(
        Guid candidateId, CancellationToken cancellationToken)
    {
        var result = await service.GetDetailsAsync(candidateId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : Ok(result.Value!);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateScoutingCandidateCommand request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : Ok(result.Value);
    }

    [HttpPost("{candidateId:guid}/observations")]
    public async Task<IActionResult> AddObservation(
        Guid candidateId, [FromBody] ScoutingObservationInput request, CancellationToken cancellationToken)
    {
        var result = await service.AddObservationAsync(candidateId, request, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    [HttpPut("{candidateId:guid}")]
    public async Task<IActionResult> UpdateCandidate(
        Guid candidateId, [FromBody] UpdateScoutingCandidateCommand request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateCandidateAsync(candidateId, request, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    [HttpPut("{candidateId:guid}/observations/{observationId:guid}")]
    public async Task<IActionResult> UpdateObservation(
        Guid candidateId, Guid observationId, [FromBody] ScoutingObservationInput request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateObservationAsync(candidateId, observationId, request, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }
    [HttpPut("{candidateId:guid}/decision")]
    public async Task<IActionResult> ChangeDecision(
        Guid candidateId, [FromBody] ChangeScoutingDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeDecisionAsync(candidateId, request.Decision, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    [HttpGet("{candidateId:guid}/registration-matches")]
    public async Task<ActionResult<IReadOnlyList<ScoutingRegistrationMatch>>> FindRegistrationMatches(
        Guid candidateId, CancellationToken cancellationToken)
    {
        var result = await service.FindRegistrationMatchesAsync(candidateId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : Ok(result.Value!);
    }

    [HttpPut("{candidateId:guid}/registration")]
    public async Task<IActionResult> LinkRegistration(
        Guid candidateId, [FromBody] LinkScoutingRegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.LinkRegistrationAsync(candidateId, request.PersonId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }
    private ObjectResult ToProblem(Error error)
    {
        var status = error.Code == ActorAuthorizationErrors.Forbidden.Code
            ? StatusCodes.Status403Forbidden
            : error.Code == ScoutingErrors.NotFound.Code || error.Code == ScoutingErrors.ObservationNotFound.Code
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
        var details = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = error.Message
        };
        details.Extensions["code"] = error.Code;
        var result = StatusCode(status, details);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
