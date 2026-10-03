using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Fixtures;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.Fixtures;

public sealed record CreateFixtureCommand(
    Guid SeasonTeamId,
    FixtureType Type,
    string Opponent,
    DateOnly Date,
    TimeOnly StartTime,
    string Venue,
    FixtureVenueSide VenueSide,
    string? Notes);

public sealed record FixtureListItem(
    Guid Id,
    Guid SeasonTeamId,
    FixtureType Type,
    string Opponent,
    DateOnly Date,
    TimeOnly StartTime,
    string Venue,
    FixtureVenueSide VenueSide,
    string? Notes,
    FixtureStatus Status);

public sealed record FixtureSquadMemberDetails(
    Guid AtmacaCardId,
    FixtureSquadRole Role);

public sealed record FixtureMatchEventDetails(
    FixtureMatchEventType Type,
    int Minute,
    Guid? AtmacaCardId,
    Guid? RelatedAtmacaCardId);

public sealed record FixtureScoreEventDetails(
    FixtureScoreSide Side,
    string ScoreTypeCode,
    int ScoreValue,
    int? Minute,
    Guid? AtmacaCardId,
    Guid? AssistAtmacaCardId);

public sealed record FixturePlayerStatisticsDetails(
    Guid AtmacaCardId,
    FixtureSquadRole Role,
    bool Started,
    int MinutesPlayed,
    int Goals,
    int Assists,
    int YellowCards,
    int RedCards);

public sealed record FixtureCorrectionDetails(
    string Reason,
    DateTime ReopenedAtUtc,
    Guid ReopenedByActorId);

public sealed record FixtureDetails(
    Guid Id,
    Guid SeasonTeamId,
    FixtureType Type,
    string Opponent,
    DateOnly Date,
    TimeOnly StartTime,
    string Venue,
    FixtureVenueSide VenueSide,
    string? Notes,
    FixtureStatus Status,
    int? DurationMinutes,
    string? Referee,
    string? MatchNotes,
    int OurScore,
    int OpponentScore,
    IReadOnlyList<FixtureSquadMemberDetails> SquadMembers,
    IReadOnlyList<FixtureMatchEventDetails> MatchEvents,
    IReadOnlyList<FixtureScoreEventDetails> ScoreEvents,
    IReadOnlyList<FixtureCorrectionDetails> Corrections,
    IReadOnlyList<FixturePlayerStatisticsDetails> PlayerStatistics);

public sealed class FixtureService(
    IActorAuthorizationService authorization,
    ICurrentActor currentActor,
    IFixtureRepository fixtures,
    ISeasonTeamRepository seasonTeams,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> CreateAsync(
        CreateFixtureCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.Create, cancellationToken);
        if (authorized.IsFailure)
            return Result<Guid>.Failure(authorized.Error!);
        if (command.SeasonTeamId == Guid.Empty)
            return Result<Guid>.Failure(FixtureErrors.SeasonTeamRequired);

        var team = await seasonTeams.GetByIdAsync(
            SeasonTeamId.From(command.SeasonTeamId), cancellationToken);
        if (team is null)
            return Result<Guid>.Failure(FixtureErrors.SeasonTeamNotFound);
        if (team.Status != SeasonTeamStatus.Active)
            return Result<Guid>.Failure(FixtureErrors.SeasonTeamInactive);

        var created = Fixture.Create(
            command.SeasonTeamId, command.Type, command.Opponent, command.Date,
            command.StartTime, command.Venue, command.VenueSide, command.Notes);
        if (created.IsFailure)
            return Result<Guid>.Failure(created.Error!);

        await fixtures.AddAsync(created.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(created.Value!.Id);
    }

    public async Task<Result<IReadOnlyList<FixtureListItem>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.List, cancellationToken);
        if (authorized.IsFailure)
            return Result<IReadOnlyList<FixtureListItem>>.Failure(authorized.Error!);

        var records = await fixtures.ListAsync(cancellationToken);
        return Result<IReadOnlyList<FixtureListItem>>.Success(records
            .Select(x => new FixtureListItem(
                x.Id, x.SeasonTeamId, x.Type, x.Opponent, x.Date,
                x.StartTime, x.Venue, x.VenueSide, x.Notes, x.Status))
            .ToList());
    }

    public async Task<Result<FixtureDetails>> GetDetailsAsync(
        Guid fixtureId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.List, cancellationToken);
        if (authorized.IsFailure)
            return Result<FixtureDetails>.Failure(authorized.Error!);

        var fixture = await fixtures.GetByIdAsync(fixtureId, cancellationToken);
        if (fixture is null)
            return Result<FixtureDetails>.Failure(FixtureErrors.NotFound);

        return Result<FixtureDetails>.Success(new FixtureDetails(
            fixture.Id,
            fixture.SeasonTeamId,
            fixture.Type,
            fixture.Opponent,
            fixture.Date,
            fixture.StartTime,
            fixture.Venue,
            fixture.VenueSide,
            fixture.Notes,
            fixture.Status,
            fixture.DurationMinutes,
            fixture.Referee,
            fixture.MatchNotes,
            fixture.OurScore,
            fixture.OpponentScore,
            fixture.SquadMembers
                .Select(x => new FixtureSquadMemberDetails(
                    x.AtmacaCardId, x.Role))
                .ToList(),
            fixture.MatchEvents
                .Select(x => new FixtureMatchEventDetails(
                    x.Type, x.Minute, x.AtmacaCardId, x.RelatedAtmacaCardId))
                .OrderBy(x => x.Minute)
                .ToList(),
            fixture.ScoreEvents
                .Select(x => new FixtureScoreEventDetails(
                    x.Side, x.ScoreTypeCode, x.ScoreValue, x.Minute,
                    x.AtmacaCardId, x.AssistAtmacaCardId))
                .OrderBy(x => x.Minute)
                .ToList(),
            fixture.Corrections
                .OrderByDescending(x => x.ReopenedAtUtc)
                .Select(x => new FixtureCorrectionDetails(
                    x.Reason, x.ReopenedAtUtc, x.ReopenedByActorId.Value))
                .ToList(),
            fixture.GetPlayerStatistics()
                .Select(x => new FixturePlayerStatisticsDetails(
                    x.AtmacaCardId, x.Role, x.Started, x.MinutesPlayed,
                    x.Goals, x.Assists, x.YellowCards, x.RedCards))
                .ToList()));
    }

    public async Task<Result> UpdateMatchDetailsAsync(
        Guid fixtureId,
        int durationMinutes,
        string? referee,
        string? matchNotes,
        IReadOnlyCollection<FixtureSquadMemberInput> squad,
        IReadOnlyCollection<FixtureMatchEventInput> matchEvents,
        IReadOnlyCollection<FixtureScoreEventInput> scoreEvents,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.Update, cancellationToken);
        if (authorized.IsFailure)
            return authorized;

        var fixture = await fixtures.GetByIdAsync(fixtureId, cancellationToken);
        if (fixture is null)
            return Result.Failure(FixtureErrors.NotFound);
        if (squad is null)
            return Result.Failure(FixtureErrors.SquadInvalid);
        if (matchEvents is null)
            return Result.Failure(FixtureErrors.MatchEventInvalid);
        if (scoreEvents is null)
            return Result.Failure(FixtureErrors.ScoreEventInvalid);

        var team = await seasonTeams.GetByIdAsync(
            SeasonTeamId.From(fixture.SeasonTeamId), cancellationToken);
        if (team is null)
            return Result.Failure(FixtureErrors.SeasonTeamNotFound);

        var eligibleCardIds = GetEligibleCardIds(team, fixture.Date);
        var previouslyRecordedSquadCardIds = fixture.SquadMembers
            .Select(x => x.AtmacaCardId)
            .ToHashSet();

        if (squad.Any(member =>
                member is null ||
                (!eligibleCardIds.Contains(member.AtmacaCardId) &&
                 !previouslyRecordedSquadCardIds.Contains(member.AtmacaCardId))))
            return Result.Failure(FixtureErrors.SquadMemberNotEligible);

        var updated = fixture.UpdateMatchDetails(
            durationMinutes, referee, matchNotes, squad, matchEvents, scoreEvents);
        if (updated.IsFailure)
            return updated;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(
        Guid fixtureId,
        CreateFixtureCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.Update, cancellationToken);
        if (authorized.IsFailure)
            return authorized;

        var fixture = await fixtures.GetByIdAsync(fixtureId, cancellationToken);
        if (fixture is null)
            return Result.Failure(FixtureErrors.NotFound);

        var team = await seasonTeams.GetByIdAsync(
            SeasonTeamId.From(command.SeasonTeamId), cancellationToken);
        if (team is null)
            return Result.Failure(FixtureErrors.SeasonTeamNotFound);
        if (team.Status != SeasonTeamStatus.Active)
            return Result.Failure(FixtureErrors.SeasonTeamInactive);
        var eligibleCardIds = GetEligibleCardIds(team, command.Date);
        if (fixture.SquadMembers.Any(x =>
                !eligibleCardIds.Contains(x.AtmacaCardId)))
        {
            return Result.Failure(FixtureErrors.SquadMemberNotEligible);
        }

        var updated = fixture.UpdateDetails(
            command.SeasonTeamId, command.Type, command.Opponent, command.Date,
            command.StartTime, command.Venue, command.VenueSide, command.Notes);
        if (updated.IsFailure)
            return updated;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(
        Guid fixtureId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.Cancel, cancellationToken);
        if (authorized.IsFailure)
            return authorized;

        var fixture = await fixtures.GetByIdAsync(fixtureId, cancellationToken);
        if (fixture is null)
            return Result.Failure(FixtureErrors.NotFound);

        var cancelled = fixture.Cancel();
        if (cancelled.IsFailure)
            return cancelled;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> CompleteAsync(
        Guid fixtureId,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.Update, cancellationToken);
        if (authorized.IsFailure)
            return authorized;

        var fixture = await fixtures.GetByIdAsync(fixtureId, cancellationToken);
        if (fixture is null)
            return Result.Failure(FixtureErrors.NotFound);

        var completed = fixture.Complete();
        if (completed.IsFailure)
            return completed;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReopenForCorrectionAsync(
        Guid fixtureId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var authorized = await authorization.AuthorizeAsync(
            Permissions.Fixtures.Update, cancellationToken);
        if (authorized.IsFailure)
            return authorized;

        var fixture = await fixtures.GetByIdAsync(fixtureId, cancellationToken);
        if (fixture is null)
            return Result.Failure(FixtureErrors.NotFound);

        var reopened = fixture.ReopenForCorrection(reason, currentActor.ActorId);
        if (reopened.IsFailure)
            return reopened;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static HashSet<Guid> GetEligibleCardIds(
        SeasonTeam team,
        DateOnly fixtureDate)
    {
        var date = fixtureDate.ToDateTime(TimeOnly.MinValue);
        return team.Memberships
            .Where(membership => membership.IsActiveOn(date))
            .Where(membership => membership.Assignments.Any(assignment =>
                assignment.Kind == SeasonTeamAssignmentKind.Classification &&
                string.Equals(
                    assignment.DisplayNameSnapshot,
                    "Sporcu",
                    StringComparison.OrdinalIgnoreCase) &&
                assignment.IsActiveOn(date)))
            .Select(membership => membership.AtmacaCardId.Value)
            .ToHashSet();
    }
}
