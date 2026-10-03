namespace ProjectAtmaca.Domain.Fixtures;

public sealed record FixtureSquadMemberInput(
    Guid AtmacaCardId,
    FixtureSquadRole Role);

public sealed record FixtureMatchEventInput(
    FixtureMatchEventType Type,
    int Minute,
    Guid? AtmacaCardId = null,
    Guid? RelatedAtmacaCardId = null);
