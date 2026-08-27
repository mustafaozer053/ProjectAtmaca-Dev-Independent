using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Domain.Decisions.Snapshots.Participations;

public readonly record struct ParticipationClassificationSnapshot
{
    public ActivityReference ActivityReference { get; }

    public AtmacaCardId AtmacaCardId { get; }

    public ParticipationStatus Status { get; }

    public ParticipationCondition? Condition { get; }

    public DateTimeOffset? JoinedAt { get; }

    public DateTimeOffset? LeftAt { get; }

    private ParticipationClassificationSnapshot(
        ActivityReference activityReference,
        AtmacaCardId atmacaCardId,
        ParticipationStatus status,
        ParticipationCondition? condition,
        DateTimeOffset? joinedAt,
        DateTimeOffset? leftAt)
    {
        ActivityReference = activityReference;
        AtmacaCardId = atmacaCardId;
        Status = status;
        Condition = condition;
        JoinedAt = joinedAt;
        LeftAt = leftAt;
    }

    public static ParticipationClassificationSnapshot Create(
        ActivityReference activityReference,
        AtmacaCardId atmacaCardId,
        ParticipationStatus status,
        ParticipationCondition? condition,
        DateTimeOffset? joinedAt,
        DateTimeOffset? leftAt)
    {
        return new ParticipationClassificationSnapshot(
            activityReference,
            atmacaCardId,
            status,
            condition,
            joinedAt,
            leftAt);
    }
}
