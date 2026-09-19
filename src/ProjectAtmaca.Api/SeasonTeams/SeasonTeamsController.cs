using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.SeasonTeams;
using ProjectAtmaca.Application.SeasonTeams.AddMembership;
using ProjectAtmaca.Application.SeasonTeams.Create;
using ProjectAtmaca.Application.SeasonTeams.List;
using ProjectAtmaca.Application.SeasonTeams.EndMembership;
using ProjectAtmaca.Application.SeasonTeams.GetById;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Api.SeasonTeams;

[ApiController]
[Route("api/season-teams")]
public sealed class SeasonTeamsController : ControllerBase
{
    private readonly CreateSeasonTeamCommandHandler _createHandler;
    private readonly ListSeasonTeamsQueryHandler _listHandler;
    private readonly AddSeasonTeamMembershipCommandHandler _membershipHandler;
    private readonly EndSeasonTeamMembershipCommandHandler _endMembershipHandler;
    private readonly GetSeasonTeamByIdQueryHandler _getByIdHandler;

    public SeasonTeamsController(
        CreateSeasonTeamCommandHandler createHandler,
        ListSeasonTeamsQueryHandler listHandler,
        AddSeasonTeamMembershipCommandHandler membershipHandler,
        EndSeasonTeamMembershipCommandHandler endMembershipHandler,
        GetSeasonTeamByIdQueryHandler getByIdHandler)
    {
        _createHandler = createHandler;
        _listHandler = listHandler;
        _membershipHandler = membershipHandler;
        _endMembershipHandler = endMembershipHandler;
        _getByIdHandler = getByIdHandler;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        CreateSeasonTeamRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createHandler.Handle(
            new CreateSeasonTeamCommand(
                request.SeasonId,
                request.OrganizationId,
                request.AgeGroupId,
                request.Name),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Created(
            $"/api/season-teams/{result.Value!.Value:D}",
            result.Value.Value);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SeasonTeamResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _listHandler.Handle(
            new ListSeasonTeamsQuery(), cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(x => new SeasonTeamResponse(
            x.Id,
            x.SeasonId,
            x.OrganizationId,
            x.AgeGroupId,
            x.Name,
            x.MembershipCount,
            x.IsActive)));
    }

    [HttpPost("{seasonTeamId}/memberships")]
    public async Task<ActionResult<Guid>> AddMembership(
        Guid seasonTeamId,
        AddSeasonTeamMembershipRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _membershipHandler.Handle(
            new AddSeasonTeamMembershipCommand(
                seasonTeamId,
                request.AtmacaCardId,
                request.StartDate,
                request.EndDate),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Created(
            $"/api/season-teams/{seasonTeamId:D}/memberships/{result.Value!.Value:D}",
            result.Value.Value);
    }

    [HttpGet("{seasonTeamId}")]
    public async Task<ActionResult<SeasonTeamDetailsResponse>> GetById(
        Guid seasonTeamId,
        CancellationToken cancellationToken)
    {
        if (seasonTeamId == Guid.Empty)
            return ToProblem(SeasonTeamApplicationErrors.NotFound);

        var result = await _getByIdHandler.Handle(
            new GetSeasonTeamByIdQuery(
                SeasonTeamId.From(seasonTeamId)),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        var details = result.Value!;
        return Ok(new SeasonTeamDetailsResponse(
            details.Id,
            details.SeasonId,
            details.OrganizationId,
            details.AgeGroupId,
            details.Name,
            details.IsActive,
            details.Memberships.Select(x =>
                new SeasonTeamMembershipResponse(
                    x.Id,
                    x.AtmacaCardId,
                    x.StartDate,
                    x.EndDate,
                    x.IsActive)).ToList()));
    }

    [HttpPost("{seasonTeamId}/memberships/{membershipId}/end")]
    public async Task<IActionResult> EndMembership(
        Guid seasonTeamId,
        Guid membershipId,
        [FromBody] EndSeasonTeamMembershipRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _endMembershipHandler.Handle(
            new EndSeasonTeamMembershipCommand(
                seasonTeamId,
                membershipId,
                request.EndDate),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        var statusCode = error.Code == SeasonTeamApplicationErrors.NotFound.Code
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = error.Message
        };
        details.Extensions["code"] = error.Code;
        var result = StatusCode(statusCode, details);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
