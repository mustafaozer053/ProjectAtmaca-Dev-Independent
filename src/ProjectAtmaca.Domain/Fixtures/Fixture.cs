using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Domain.Fixtures;

public sealed class Fixture : AuditableAggregateRoot
{
    private readonly List<FixtureSquadMember> _squadMembers = [];
    private readonly List<FixtureMatchEvent> _matchEvents = [];
    private readonly List<FixtureScoreEvent> _scoreEvents = [];
    private readonly List<FixtureCorrection> _corrections = [];

    public Guid SeasonTeamId { get; private set; }
    public FixtureType Type { get; private set; }
    public string Opponent { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public string Venue { get; private set; } = null!;
    public FixtureVenueSide VenueSide { get; private set; }
    public string? Notes { get; private set; }
    public FixtureStatus Status { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string? Referee { get; private set; }
    public string? MatchNotes { get; private set; }
    public IReadOnlyCollection<FixtureSquadMember> SquadMembers =>
        _squadMembers.AsReadOnly();
    public IReadOnlyCollection<FixtureMatchEvent> MatchEvents =>
        _matchEvents.AsReadOnly();
    public IReadOnlyCollection<FixtureScoreEvent> ScoreEvents =>
        _scoreEvents.AsReadOnly();
    public IReadOnlyCollection<FixtureCorrection> Corrections =>
        _corrections.AsReadOnly();
    public int OurScore => _scoreEvents
        .Where(x => x.Side == FixtureScoreSide.SeasonTeam)
        .Sum(x => x.ScoreValue);
    public int OpponentScore => _scoreEvents
        .Where(x => x.Side == FixtureScoreSide.Opponent)
        .Sum(x => x.ScoreValue);

    private Fixture() { }

    private Fixture(
        Guid id,
        Guid seasonTeamId,
        FixtureType type,
        string opponent,
        DateOnly date,
        TimeOnly startTime,
        string venue,
        FixtureVenueSide venueSide,
        string? notes) : base(id)
    {
        SeasonTeamId = seasonTeamId;
        Type = type;
        Opponent = opponent;
        Date = date;
        StartTime = startTime;
        Venue = venue;
        VenueSide = venueSide;
        Notes = notes;
        Status = FixtureStatus.Scheduled;
    }

    public static Result<Fixture> Create(
        Guid seasonTeamId,
        FixtureType type,
        string opponent,
        DateOnly date,
        TimeOnly startTime,
        string venue,
        FixtureVenueSide venueSide,
        string? notes)
    {
        var validation = ValidateDetails(
            seasonTeamId, type, opponent, date, venue, venueSide, notes);
        if (validation.IsFailure)
            return Result<Fixture>.Failure(validation.Error!);

        return Result<Fixture>.Success(new Fixture(
            Guid.NewGuid(),
            seasonTeamId,
            type,
            opponent.Trim(),
            date,
            startTime,
            venue.Trim(),
            venueSide,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()));
    }

    public Result UpdateDetails(
        Guid seasonTeamId,
        FixtureType type,
        string opponent,
        DateOnly date,
        TimeOnly startTime,
        string venue,
        FixtureVenueSide venueSide,
        string? notes)
    {
        if (Status == FixtureStatus.Cancelled)
            return Result.Failure(FixtureErrors.CancelledCannotBeEdited);
        if (Status == FixtureStatus.Completed)
            return Result.Failure(FixtureErrors.CompletedCannotBeEdited);

        var validation = ValidateDetails(
            seasonTeamId, type, opponent, date, venue, venueSide, notes);
        if (validation.IsFailure)
            return validation;

        SeasonTeamId = seasonTeamId;
        Type = type;
        Opponent = opponent.Trim();
        Date = date;
        StartTime = startTime;
        Venue = venue.Trim();
        VenueSide = venueSide;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == FixtureStatus.Completed)
            return Result.Failure(FixtureErrors.CompletedCannotBeCancelled);
        if (Status == FixtureStatus.CorrectionInProgress)
            return Result.Failure(FixtureErrors.CorrectionInProgressCannotBeCancelled);
        if (Status == FixtureStatus.Cancelled)
            return Result.Success();

        Status = FixtureStatus.Cancelled;
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status == FixtureStatus.Cancelled)
            return Result.Failure(FixtureErrors.CancelledCannotBeCompleted);
        if (Status == FixtureStatus.Completed)
            return Result.Success();
        if (DurationMinutes is null)
            return Result.Failure(FixtureErrors.MatchDetailsRequired);

        Status = FixtureStatus.Completed;
        return Result.Success();
    }

    public Result ReopenForCorrection(string? reason, ActorId actorId)
    {
        if (Status != FixtureStatus.Completed)
            return Result.Failure(FixtureErrors.OnlyCompletedCanBeReopened);
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(FixtureErrors.CorrectionReasonRequired);
        if (reason.Trim().Length > FixtureCorrection.MaxReasonLength)
            return Result.Failure(FixtureErrors.CorrectionReasonTooLong);
        if (actorId.Value == Guid.Empty)
            return Result.Failure(FixtureErrors.CorrectionActorRequired);

        _corrections.Add(new FixtureCorrection(
            Guid.NewGuid(), reason.Trim(), DateTime.UtcNow, actorId));
        Status = FixtureStatus.CorrectionInProgress;
        return Result.Success();
    }

    public Result UpdateMatchDetails(
        int durationMinutes,
        string? referee,
        string? matchNotes,
        IReadOnlyCollection<FixtureSquadMemberInput> squad,
        IReadOnlyCollection<FixtureMatchEventInput> matchEvents,
        IReadOnlyCollection<FixtureScoreEventInput> scoreEvents)
    {
        if (Status == FixtureStatus.Cancelled)
            return Result.Failure(FixtureErrors.CancelledCannotBeEdited);
        if (Status == FixtureStatus.Completed)
            return Result.Failure(FixtureErrors.CompletedCannotBeEdited);
        if (durationMinutes is < 1 or > 180)
            return Result.Failure(FixtureErrors.DurationInvalid);
        if (referee?.Length > 200)
            return Result.Failure(FixtureErrors.RefereeTooLong);
        if (matchNotes?.Length > 2000)
            return Result.Failure(FixtureErrors.MatchNotesTooLong);
        if (squad is null || squad.Any(x =>
                x is null ||
                x.AtmacaCardId == Guid.Empty ||
                !Enum.IsDefined(x.Role)) ||
            squad.Select(x => x.AtmacaCardId).Distinct().Count() != squad.Count)
        {
            return Result.Failure(FixtureErrors.SquadInvalid);
        }

        var squadIds = squad.Select(x => x.AtmacaCardId).ToHashSet();
        var squadRoles = squad.ToDictionary(x => x.AtmacaCardId, x => x.Role);
        if (matchEvents is null || matchEvents.Any(x =>
                !IsValidMatchEvent(x, squadIds, squadRoles)))
        {
            return Result.Failure(FixtureErrors.MatchEventInvalid);
        }
        if (scoreEvents is null || scoreEvents.Any(x =>
                !IsValidScoreEvent(x, squadIds)))
        {
            return Result.Failure(FixtureErrors.ScoreEventInvalid);
        }

        DurationMinutes = durationMinutes;
        Referee = Normalize(referee);
        MatchNotes = Normalize(matchNotes);
        _squadMembers.Clear();
        _squadMembers.AddRange(squad.Select(x =>
            new FixtureSquadMember(Guid.NewGuid(), x.AtmacaCardId, x.Role)));
        _matchEvents.Clear();
        _matchEvents.AddRange(matchEvents.Select(x =>
            new FixtureMatchEvent(
                Guid.NewGuid(), x.Type, x.Minute,
                x.AtmacaCardId, x.RelatedAtmacaCardId)));
        _scoreEvents.Clear();
        _scoreEvents.AddRange(scoreEvents.Select(x =>
            new FixtureScoreEvent(
                Guid.NewGuid(), x.Side, x.ScoreTypeCode.Trim().ToUpperInvariant(),
                x.ScoreValue, x.Minute, x.AtmacaCardId)));

        return Result.Success();
    }

    private static bool IsValidScoreEvent(
        FixtureScoreEventInput? item,
        IReadOnlySet<Guid> squadIds)
    {
        if (item is null ||
            !Enum.IsDefined(item.Side) ||
            string.IsNullOrWhiteSpace(item.ScoreTypeCode) ||
            item.ScoreTypeCode.Length > 50 ||
            item.ScoreTypeCode.Any(c =>
                !char.IsAsciiLetterOrDigit(c) && c != '_') ||
            item.ScoreValue is < 1 or > 1000 ||
            item.Minute is < 1 or > 180)
        {
            return false;
        }

        return item.AtmacaCardId is null ||
               item.Side == FixtureScoreSide.Opponent ||
               squadIds.Contains(item.AtmacaCardId.Value);
    }

    private static bool IsValidMatchEvent(
        FixtureMatchEventInput? item,
        IReadOnlySet<Guid> squadIds,
        IReadOnlyDictionary<Guid, FixtureSquadRole> squadRoles)
    {
        if (item is null ||
            !Enum.IsDefined(item.Type) ||
            item.Minute is < 1 or > 180)
            return false;

        return item.Type switch
        {
            FixtureMatchEventType.Substitution =>
                item.AtmacaCardId is Guid outgoing &&
                item.RelatedAtmacaCardId is Guid incoming &&
                outgoing != incoming &&
                squadIds.Contains(outgoing) &&
                squadIds.Contains(incoming) &&
                squadRoles[outgoing] == FixtureSquadRole.Starter &&
                squadRoles[incoming] == FixtureSquadRole.Substitute,
            FixtureMatchEventType.YellowCard or FixtureMatchEventType.RedCard =>
                item.AtmacaCardId is Guid playerId &&
                item.RelatedAtmacaCardId is null &&
                squadIds.Contains(playerId),
            _ => false
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result ValidateDetails(
        Guid seasonTeamId,
        FixtureType type,
        string opponent,
        DateOnly date,
        string venue,
        FixtureVenueSide venueSide,
        string? notes)
    {
        if (seasonTeamId == Guid.Empty)
            return Result.Failure(FixtureErrors.SeasonTeamRequired);
        if (!Enum.IsDefined(type))
            return Result.Failure(FixtureErrors.TypeInvalid);
        if (string.IsNullOrWhiteSpace(opponent) || opponent.Trim().Length > 120)
            return Result.Failure(FixtureErrors.OpponentInvalid);
        if (date == default)
            return Result.Failure(FixtureErrors.DateRequired);
        if (!Enum.IsDefined(venueSide))
            return Result.Failure(FixtureErrors.VenueSideInvalid);
        if (string.IsNullOrWhiteSpace(venue) || venue.Trim().Length > 200)
            return Result.Failure(FixtureErrors.VenueInvalid);
        if (notes?.Length > 1000)
            return Result.Failure(FixtureErrors.NotesTooLong);
        return Result.Success();
    }
}
