namespace ProjectAtmaca.Domain.Decisions;

public readonly record struct DecisionRevision
{
    public int Value { get; }

    private DecisionRevision(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Decision revision must be greater than zero.");
        }

        Value = value;
    }

    public static DecisionRevision Initial
    {
        get
        {
            return new DecisionRevision(1);
        }
    }

    public static DecisionRevision From(int value)
    {
        return new DecisionRevision(value);
    }

    public DecisionRevision Next()
    {
        return new DecisionRevision(
            checked(Value + 1));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
