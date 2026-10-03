namespace ProjectAtmaca.Domain.Fixtures;

public enum FixtureMatchEventType
{
    // Legacy values are retained so existing match-event rows can be migrated.
    Goal = 1,
    OpponentGoal = 2,
    Substitution = 3,
    YellowCard = 4,
    RedCard = 5
}
