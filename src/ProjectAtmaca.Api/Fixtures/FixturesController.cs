using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Fixtures;
using ProjectAtmaca.Api.Fixtures.MatchDetails;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Api.Fixtures;

[ApiController]
[Route("api/fixtures")]
public sealed class FixturesController(FixtureService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FixtureResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToList());
    }

    [HttpGet("{fixtureId:guid}")]
    public async Task<ActionResult<FixtureDetailsResponse>> GetDetails(
        Guid fixtureId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDetailsAsync(fixtureId, cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        var details = result.Value!;
        var fixture = new FixtureListItem(
            details.Id, details.SeasonTeamId, details.Type, details.Opponent,
            details.Date, details.StartTime, details.Venue, details.VenueSide,
            details.Notes, details.Status);
        return Ok(new FixtureDetailsResponse(
            ToResponse(fixture),
            details.DurationMinutes,
            details.Referee,
            details.MatchNotes,
            details.OurScore,
            details.OpponentScore,
            details.SquadMembers,
            details.MatchEvents,
            details.ScoreEvents,
            details.Corrections));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateFixtureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            new CreateFixtureCommand(
                request.SeasonTeamId, request.Type, request.Opponent,
                request.Date, request.StartTime, request.Venue,
                request.VenueSide, request.Notes),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Created($"/api/fixtures/{result.Value:D}", result.Value);
    }

    [HttpPut("{fixtureId:guid}")]
    public async Task<IActionResult> Update(
        Guid fixtureId,
        [FromBody] CreateFixtureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            fixtureId,
            new CreateFixtureCommand(
                request.SeasonTeamId, request.Type, request.Opponent,
                request.Date, request.StartTime, request.Venue,
                request.VenueSide, request.Notes),
            cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    [HttpPost("{fixtureId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid fixtureId,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(fixtureId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    [HttpPost("{fixtureId:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid fixtureId,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAsync(fixtureId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    [HttpPost("{fixtureId:guid}/reopen-for-correction")]
    public async Task<IActionResult> ReopenForCorrection(
        Guid fixtureId,
        [FromBody] ReopenFixtureForCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ReopenForCorrectionAsync(
            fixtureId, request.Reason, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    [HttpPut("{fixtureId:guid}/match-details")]
    public async Task<IActionResult> UpdateMatchDetails(
        Guid fixtureId,
        [FromBody] UpdateFixtureMatchDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateMatchDetailsAsync(
            fixtureId,
            request.DurationMinutes,
            request.Referee,
            request.MatchNotes,
            request.Squad,
            request.MatchEvents,
            request.ScoreEvents,
            cancellationToken);
        return result.IsFailure ? ToProblem(result.Error!) : NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        var status = error.Code == ActorAuthorizationErrors.Forbidden.Code
            ? StatusCodes.Status403Forbidden
            : error.Code == FixtureErrors.SeasonTeamNotFound.Code ||
              error.Code == FixtureErrors.NotFound.Code
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

    private static FixtureResponse ToResponse(FixtureListItem x) =>
        new(x.Id, x.SeasonTeamId, x.Type, x.Opponent, x.Date, x.StartTime,
            x.Venue, x.VenueSide, x.Notes, x.Status);
}
