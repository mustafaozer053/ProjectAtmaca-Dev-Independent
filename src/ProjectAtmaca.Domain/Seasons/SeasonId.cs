namespace ProjectAtmaca.Domain.Seasons;

public readonly record struct SeasonId
{
    public Guid Value { get; }

    private SeasonId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Season id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static SeasonId New()
    {
        return new SeasonId(Guid.NewGuid());
    }

    public static SeasonId From(Guid value)
    {
        return new SeasonId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
