namespace ProjectAtmaca.Domain.Decisions;

public readonly record struct DecisionApplicationId
{
    public Guid Value { get; }

    private DecisionApplicationId(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Decision application id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static DecisionApplicationId New()
    {
        return new DecisionApplicationId(
            Guid.NewGuid());
    }

    public static DecisionApplicationId From(
        Guid value)
    {
        return new DecisionApplicationId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
