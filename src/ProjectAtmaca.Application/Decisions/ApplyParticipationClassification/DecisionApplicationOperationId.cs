namespace ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

public readonly record struct DecisionApplicationOperationId
{
    public Guid Value { get; }

    private DecisionApplicationOperationId(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Decision application operation ID cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static DecisionApplicationOperationId New() =>
        new(Guid.NewGuid());

    public static DecisionApplicationOperationId From(
        Guid value) =>
        new(value);

    public override string ToString() =>
        Value.ToString();
}
