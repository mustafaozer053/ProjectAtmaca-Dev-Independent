namespace ProjectAtmaca.Domain.Decisions;

public readonly record struct DecisionId
{
    public Guid Value { get; }

    private DecisionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Decision id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static DecisionId New()
    {
        return new DecisionId(Guid.NewGuid());
    }

    public static DecisionId From(Guid value)
    {
        return new DecisionId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
