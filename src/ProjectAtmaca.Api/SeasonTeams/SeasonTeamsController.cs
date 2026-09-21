using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.SeasonTeams;
using ProjectAtmaca.Application.SeasonTeams.AddMembership;
using ProjectAtmaca.Application.SeasonTeams.Create;
using ProjectAtmaca.Application.SeasonTeams.ChangeStatus;
using ProjectAtmaca.Application.SeasonTeams.List;
using ProjectAtmaca.Application.SeasonTeams.EndMembership;
using ProjectAtmaca.Application.SeasonTeams.GetById;
using ProjectAtmaca.Application.SeasonTeams.AddMembershipAssignment;
using ProjectAtmaca.Application.SeasonTeams.EndMembershipAssignment;
using ProjectAtmaca.Application.Abstractions.Security;
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
    private readonly AddSeasonTeamMembershipAssignmentCommandHandler _addAssignmentHandler;
    private readonly EndSeasonTeamMembershipAssignmentCommandHandler _endAssignmentHandler;
    private readonly GetSeasonTeamByIdQueryHandler _getByIdHandler;
    private readonly ChangeSeasonTeamStatusCommandHandler _statusHandler;

    public SeasonTeamsController(
        CreateSeasonTeamCommandHandler createHandler,
        ListSeasonTeamsQueryHandler listHandler,
        AddSeasonTeamMembershipCommandHandler membershipHandler,
        EndSeasonTeamMembershipCommandHandler endMembershipHandler,
        AddSeasonTeamMembershipAssignmentCommandHandler addAssignmentHandler,
        EndSeasonTeamMembershipAssignmentCommandHandler endAssignmentHandler,
        GetSeasonTeamByIdQueryHandler getByIdHandler,
        ChangeSeasonTeamStatusCommandHandler statusHandler)
    {
        _createHandler = createHandler;
        _listHandler = listHandler;
        _membershipHandler = membershipHandler;
        _endMembershipHandler = endMembershipHandler;
        _addAssignmentHandler = addAssignmentHandler;
        _endAssignmentHandler = endAssignmentHandler;
        _getByIdHandler = getByIdHandler;
        _statusHandler = statusHandler;
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

        return Ok(ToDetailsResponse(result.Value!));
    }

    [HttpGet("{seasonTeamId}/roster-view")]
    public async Task<ActionResult<SeasonTeamRosterViewResponse>> GetRosterView(
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
        var memberships = details.Memberships
            .Select(ToMembershipResponse)
            .ToList();

        var groups = memberships
            .SelectMany(membership =>
                membership.Assignments
                    .Where(assignment =>
                        assignment.IsActive &&
                        IsClassificationKind(assignment.Kind))
                    .Select(assignment => new
                    {
                        Classification = assignment.DisplayNameSnapshot,
                        Membership = membership
                    }))
            .GroupBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SeasonTeamRosterGroupResponse(
                group.First().Classification,
                group.Select(x => x.Membership)
                    .DistinctBy(x => x.Id)
                    .Count(),
                group.Select(x => x.Membership)
                    .DistinctBy(x => x.Id)
                    .OrderBy(x => x.StartDate)
                    .ThenBy(x => x.AtmacaCardId)
                    .ToList()))
            .OrderBy(x => x.Classification)
            .ToList();

        HashSet<Guid> classifiedMembershipIds = groups
            .SelectMany(x => x.Memberships)
            .Select(x => x.Id)
            .ToHashSet();

        var unclassified = memberships
            .Where(x => !classifiedMembershipIds.Contains(x.Id))
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.AtmacaCardId)
            .ToList();

        var classificationCounts = groups
            .Select(x => new SeasonTeamRosterClassificationCountResponse(
                x.Classification,
                x.MembershipCount))
            .ToList();

        int playerCount = groups
            .Where(x => string.Equals(
                x.Classification,
                "Sporcu",
                StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.MembershipCount);
        int technicalStaffCount = groups
            .Where(x => string.Equals(
                x.Classification,
                "Teknik Ekip",
                StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.MembershipCount);

        var primarySections = new[]
        {
            new SeasonTeamRosterPrimarySectionResponse(
                "players",
                "Sporcu",
                playerCount,
                playerCount > 0),
            new SeasonTeamRosterPrimarySectionResponse(
                "technical-staff",
                "Teknik Ekip",
                technicalStaffCount,
                technicalStaffCount > 0)
        };

        return Ok(new SeasonTeamRosterViewResponse(
            details.Id,
            memberships.Count,
            memberships.Count(x => x.IsActive),
            memberships.Count(x => !x.IsActive),
            unclassified.Count,
            primarySections,
            groups,
            classificationCounts,
            unclassified));
    }

    [HttpPatch("{seasonTeamId}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid seasonTeamId,
        ChangeSeasonTeamStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (seasonTeamId == Guid.Empty)
            return ToProblem(SeasonTeamApplicationErrors.NotFound);

        var result = await _statusHandler.Handle(
            new ChangeSeasonTeamStatusCommand(
                SeasonTeamId.From(seasonTeamId),
                request.IsActive),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
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

    [HttpPost("{seasonTeamId}/memberships/{membershipId}/assignments")]
    public async Task<ActionResult<Guid>> AddMembershipAssignment(
        Guid seasonTeamId,
        Guid membershipId,
        [FromBody] AddSeasonTeamMembershipAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addAssignmentHandler.Handle(
            new AddSeasonTeamMembershipAssignmentCommand(
                seasonTeamId,
                membershipId,
                request.Kind,
                request.DefinitionId,
                request.DisplayNameSnapshot,
                request.StartDate,
                request.EndDate),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Created(
            $"/api/season-teams/{seasonTeamId:D}/memberships/{membershipId:D}" +
            $"/assignments/{result.Value!.Value:D}",
            result.Value.Value);
    }

    [HttpPost("{seasonTeamId}/memberships/{membershipId}/assignments/{assignmentId}/end")]
    public async Task<IActionResult> EndMembershipAssignment(
        Guid seasonTeamId,
        Guid membershipId,
        Guid assignmentId,
        [FromBody] EndSeasonTeamMembershipAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _endAssignmentHandler.Handle(
            new EndSeasonTeamMembershipAssignmentCommand(
                seasonTeamId,
                membershipId,
                assignmentId,
                request.EndDate),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        var statusCode = error.Code == ActorAuthorizationErrors.Forbidden.Code
            ? StatusCodes.Status403Forbidden
            : error.Code == SeasonTeamApplicationErrors.NotFound.Code
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

    private static SeasonTeamDetailsResponse ToDetailsResponse(
        SeasonTeamDetails details)
    {
        return new SeasonTeamDetailsResponse(
            details.Id,
            details.SeasonId,
            details.OrganizationId,
            details.AgeGroupId,
            details.Name,
            details.IsActive,
            details.Memberships.Select(ToMembershipResponse).ToList());
    }

    private static SeasonTeamMembershipResponse ToMembershipResponse(
        SeasonTeamMembershipDetails membership)
    {
        return new SeasonTeamMembershipResponse(
            membership.Id,
            membership.AtmacaCardId,
            membership.StartDate,
            membership.EndDate,
            membership.IsActive,
            membership.Assignments.Select(assignment =>
                new SeasonTeamMembershipAssignmentResponse(
                    assignment.Id,
                    assignment.Kind.ToString(),
                    assignment.DefinitionId,
                    assignment.DisplayNameSnapshot,
                    assignment.StartDate,
                    assignment.EndDate,
                    assignment.IsActive)).ToList());
    }

    private static bool IsClassificationKind(string kind)
    {
        return string.Equals(
            kind,
            SeasonTeamAssignmentKind.Classification.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}
