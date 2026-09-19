namespace ProjectAtmaca.Domain.SeasonTeams;

public readonly record struct SeasonTeamMembershipId
{
    public Guid Value { get; }

    private SeasonTeamMembershipId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Season team membership id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static SeasonTeamMembershipId New()
    {
        return new SeasonTeamMembershipId(Guid.NewGuid());
    }

    public static SeasonTeamMembershipId From(Guid value)
    {
        return new SeasonTeamMembershipId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
