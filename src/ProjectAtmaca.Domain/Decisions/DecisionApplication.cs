using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Decisions;

public sealed class DecisionApplication : Entity
{
    public DecisionApplicationId DecisionApplicationId =>
        DecisionApplicationId.From(Id);

    public DecisionId DecisionId { get; private set; }

    public DecisionTargetReference Target { get; private set; }

    public DecisionRevision AppliedDecisionRevision { get; private set; }

    public DateTimeOffset AppliedAtUtc { get; private set; }

    private DecisionApplication()
    {
    }

    private DecisionApplication(
        DecisionApplicationId id,
        DecisionId decisionId,
        DecisionTargetReference target,
        DecisionRevision appliedDecisionRevision,
        DateTimeOffset appliedAtUtc)
        : base(id.Value)
    {
        DecisionId = decisionId;
        Target = target;
        AppliedDecisionRevision =
            appliedDecisionRevision;
        AppliedAtUtc = appliedAtUtc;
    }

    public static DecisionApplication Create(
        DecisionId decisionId,
        DecisionTargetReference target,
        DecisionRevision appliedDecisionRevision,
        DateTimeOffset appliedAtUtc)
    {
        if (decisionId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Decision id cannot be empty.",
                nameof(decisionId));
        }

        if (!Enum.IsDefined(target.TargetType) ||
            target.TargetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Decision target is invalid.",
                nameof(target));
        }

        if (appliedDecisionRevision.Value < 1)
        {
            throw new ArgumentException(
                "Applied decision revision must be valid.",
                nameof(appliedDecisionRevision));
        }

        if (appliedAtUtc == default)
        {
            throw new ArgumentException(
                "Applied time cannot be default.",
                nameof(appliedAtUtc));
        }

        if (appliedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Applied time must be UTC.",
                nameof(appliedAtUtc));
        }

        return new DecisionApplication(
            DecisionApplicationId.New(),
            decisionId,
            target,
            appliedDecisionRevision,
            appliedAtUtc);
    }
}
