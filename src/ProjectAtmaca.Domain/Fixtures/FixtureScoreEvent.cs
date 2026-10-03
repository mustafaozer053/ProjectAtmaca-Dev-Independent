using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Fixtures;

public sealed class FixtureScoreEvent : Entity
{
    public FixtureScoreSide Side { get; private set; }
    public string ScoreTypeCode { get; private set; } = null!;
    public int ScoreValue { get; private set; }
    public int? Minute { get; private set; }
    public Guid? AtmacaCardId { get; private set; }
    public Guid? AssistAtmacaCardId { get; private set; }

    private FixtureScoreEvent() { }

    internal FixtureScoreEvent(
        Guid id,
        FixtureScoreSide side,
        string scoreTypeCode,
        int scoreValue,
        int? minute,
        Guid? atmacaCardId,
        Guid? assistAtmacaCardId = null) : base(id)
    {
        Side = side;
        ScoreTypeCode = scoreTypeCode;
        ScoreValue = scoreValue;
        Minute = minute;
        AtmacaCardId = atmacaCardId;
        AssistAtmacaCardId = assistAtmacaCardId;
    }
}
