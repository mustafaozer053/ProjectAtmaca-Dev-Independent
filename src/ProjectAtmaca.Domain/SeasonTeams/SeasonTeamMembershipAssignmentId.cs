namespace ProjectAtmaca.Domain.SeasonTeams;

public readonly record struct SeasonTeamMembershipAssignmentId
{
    public Guid Value { get; }

    private SeasonTeamMembershipAssignmentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Season team membership assignment id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static SeasonTeamMembershipAssignmentId New()
    {
        return new SeasonTeamMembershipAssignmentId(Guid.NewGuid());
    }

    public static SeasonTeamMembershipAssignmentId From(Guid value)
    {
        return new SeasonTeamMembershipAssignmentId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
