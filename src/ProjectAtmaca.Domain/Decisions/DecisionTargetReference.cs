using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Domain.Decisions;

public readonly record struct DecisionTargetReference
{
    public DecisionTargetType TargetType { get; }

    public Guid TargetId { get; }

    private DecisionTargetReference(
        DecisionTargetType targetType,
        Guid targetId)
    {
        TargetType = targetType;
        TargetId = targetId;
    }

    public static DecisionTargetReference ForParticipation(
        ParticipationId participationId)
    {
        return new DecisionTargetReference(
            DecisionTargetType.Participation,
            participationId.Value);
    }

    public override string ToString()
    {
        return $"{TargetType}:{TargetId}";
    }
}
