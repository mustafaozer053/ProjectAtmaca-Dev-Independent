namespace ProjectAtmaca.Domain.SeasonTeams;

public readonly record struct SeasonTeamId
{
    public Guid Value { get; }

    private SeasonTeamId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Season team id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static SeasonTeamId New()
    {
        return new SeasonTeamId(Guid.NewGuid());
    }

    public static SeasonTeamId From(Guid value)
    {
        return new SeasonTeamId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
