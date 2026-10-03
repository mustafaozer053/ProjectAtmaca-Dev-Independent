namespace ProjectAtmaca.Domain.Fixtures;

public sealed record FixturePlayerStatistics(
    Guid AtmacaCardId,
    FixtureSquadRole Role,
    bool Started,
    int MinutesPlayed,
    int Goals,
    int Assists,
    int YellowCards,
    int RedCards)
{
    public bool Appeared => MinutesPlayed > 0;
}
