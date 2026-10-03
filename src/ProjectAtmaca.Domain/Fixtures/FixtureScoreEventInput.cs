namespace ProjectAtmaca.Domain.Fixtures;

public sealed record FixtureScoreEventInput(
    FixtureScoreSide Side,
    string ScoreTypeCode,
    int ScoreValue,
    int? Minute,
    Guid? AtmacaCardId,
    Guid? AssistAtmacaCardId = null);