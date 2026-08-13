using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations.DomainEvents;

namespace ProjectAtmaca.Domain.Participations;

public sealed class Participation : AuditableAggregateRoot
{
    public ParticipationId ParticipationId =>
        ParticipationId.From(Id);

    public ActivityReference ActivityReference { get; private set; }

    public AtmacaCardId AtmacaCardId { get; private set; }

    public ParticipationStatus Status { get; private set; }

    public DateTimeOffset? JoinedAt { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    public ParticipationNote? Note { get; private set; }

    public ParticipationCondition? Condition { get; private set; }

    private Participation()
    {
        ActivityReference = null!;
    }

    private Participation(
        ParticipationId id,
        ActivityReference activityReference,
        AtmacaCardId atmacaCardId)
        : base(id.Value)
    {
        ActivityReference = activityReference;
        AtmacaCardId = atmacaCardId;

        Status = ParticipationStatus.NotRecorded;
        Condition = null;
    }

    public static Result<Participation> Create(
        ActivityReference activityReference,
        AtmacaCardId atmacaCardId)
    {
        if (activityReference is null)
        {
            return Result<Participation>.Failure(
                ParticipationErrors.ActivityReferenceRequired);
        }

        if (atmacaCardId.Value == Guid.Empty)
        {
            return Result<Participation>.Failure(
                ParticipationErrors.AtmacaCardRequired);
        }

        var participation = new Participation(
            ParticipationId.New(),
            activityReference,
            atmacaCardId);

        participation.RaiseDomainEvent(
            new ParticipationCreatedDomainEvent(
                participation.ParticipationId,
                participation.ActivityReference,
                participation.AtmacaCardId));

        return Result<Participation>.Success(
            participation);
    }

    private static Result ValidateCondition(
        ParticipationStatus status,
        ParticipationCondition? condition)
    {
        if (condition is null)
        {
            return Result.Success();
        }

        if (!condition.IsApplicableTo(status))
        {
            return Result.Failure(
                ParticipationErrors.InvalidConditionForStatus);
        }

        return Result.Success();
    }

    public Result MarkPresent(
        ParticipationCondition? condition = null)
    {
        Result validationResult =
            ValidateCondition(
                ParticipationStatus.Present,
                condition);

        if (validationResult.IsFailure)
        {
            return Result.Failure(
                validationResult.Error!);
        }

        if (Status == ParticipationStatus.Absent)
        {
            return Result.Failure(
                ParticipationErrors
                    .ClassificationCorrectionRequired);
        }

        bool statusUnchanged =
            Status == ParticipationStatus.Present;

        bool conditionUnchanged =
            Condition == condition;

        if (statusUnchanged &&
            conditionUnchanged)
        {
            return Result.Success();
        }

        Status = ParticipationStatus.Present;
        Condition = condition;

        RaiseDomainEvent(
            new ParticipationMarkedPresentDomainEvent(
                ParticipationId));

        return Result.Success();
    }

    public Result MarkAbsent(
        ParticipationCondition? condition = null)
    {
        Result validationResult =
            ValidateCondition(
                ParticipationStatus.Absent,
                condition);

        if (validationResult.IsFailure)
        {
            return Result.Failure(
                validationResult.Error!);
        }

        if (Status == ParticipationStatus.Present)
        {
            return Result.Failure(
                ParticipationErrors
                    .ClassificationCorrectionRequired);
        }

        if (JoinedAt.HasValue ||
            LeftAt.HasValue)
        {
            return Result.Failure(
                ParticipationErrors
                    .ClassificationCorrectionRequired);
        }

        bool statusUnchanged =
            Status == ParticipationStatus.Absent;

        bool conditionUnchanged =
            Condition == condition;

        if (statusUnchanged &&
            conditionUnchanged)
        {
            return Result.Success();
        }

        Status = ParticipationStatus.Absent;
        Condition = condition;

        return Result.Success();
    }

    public Result RecordArrival(
        DateTimeOffset joinedAt)
    {
        if (Status == ParticipationStatus.Absent)
        {
            return Result.Failure(
                ParticipationErrors
                    .ArrivalCannotBeRecordedWhenAbsent);
        }

        if (JoinedAt.HasValue)
        {
            if (JoinedAt.Value == joinedAt)
            {
                return Result.Success();
            }

            return Result.Failure(
                ParticipationErrors
                    .ArrivalCorrectionRequired);
        }

        if (LeftAt.HasValue &&
            joinedAt > LeftAt.Value)
        {
            return Result.Failure(
                ParticipationErrors
                    .ArrivalCannotBeAfterDeparture);
        }

        JoinedAt = joinedAt;

        return Result.Success();
    }

    public Result RecordDeparture(
        DateTimeOffset leftAt)
    {
        if (Status == ParticipationStatus.Absent)
        {
            return Result.Failure(
                ParticipationErrors
                    .DepartureCannotBeRecordedWhenAbsent);
        }

        if (JoinedAt is null)
        {
            return Result.Failure(
                ParticipationErrors
                    .ArrivalRequiredBeforeDeparture);
        }

        if (leftAt < JoinedAt.Value)
        {
            return Result.Failure(
                ParticipationErrors
                    .DepartureCannotBeBeforeArrival);
        }

        if (LeftAt.HasValue)
        {
            if (LeftAt.Value == leftAt)
            {
                return Result.Success();
            }

            return Result.Failure(
                ParticipationErrors
                    .DepartureCorrectionRequired);
        }

        LeftAt = leftAt;

        return Result.Success();
    }

    public Result CorrectArrival(
        DateTimeOffset correctedJoinedAt,
        ParticipationCorrectionReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (!JoinedAt.HasValue)
        {
            return Result.Failure(
                ParticipationErrors.ArrivalNotRecorded);
        }

        if (JoinedAt.Value == correctedJoinedAt)
        {
            return Result.Success();
        }

        if (LeftAt.HasValue &&
            correctedJoinedAt > LeftAt.Value)
        {
            return Result.Failure(
                ParticipationErrors
                    .ArrivalCannotBeAfterDeparture);
        }

        DateTimeOffset previousJoinedAt =
            JoinedAt.Value;

        JoinedAt = correctedJoinedAt;

        RaiseDomainEvent(
            new ParticipationArrivalCorrectedDomainEvent(
                ParticipationId,
                previousJoinedAt,
                correctedJoinedAt,
                reason));

        return Result.Success();
    }

    public Result CorrectDeparture(
        DateTimeOffset correctedLeftAt,
        ParticipationCorrectionReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (!LeftAt.HasValue)
        {
            return Result.Failure(
                ParticipationErrors.DepartureNotRecorded);
        }

        if (LeftAt.Value == correctedLeftAt)
        {
            return Result.Success();
        }

        if (JoinedAt is null)
        {
            return Result.Failure(
                ParticipationErrors
                    .ArrivalRequiredBeforeDeparture);
        }

        if (correctedLeftAt < JoinedAt.Value)
        {
            return Result.Failure(
                ParticipationErrors
                    .DepartureCannotBeBeforeArrival);
        }

        DateTimeOffset previousLeftAt =
            LeftAt.Value;

        LeftAt = correctedLeftAt;

        RaiseDomainEvent(
            new ParticipationDepartureCorrectedDomainEvent(
                ParticipationId,
                previousLeftAt,
                correctedLeftAt,
                reason));

        return Result.Success();
    }

    public void UpdateNote(
        ParticipationNote? note)
    {
        if (Note == note)
        {
            return;
        }

        Note = note;
    }
}