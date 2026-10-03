using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Fixtures;

public sealed class FixtureMatchEvent : Entity
{
    public FixtureMatchEventType Type { get; private set; }
    public int Minute { get; private set; }
    public Guid? AtmacaCardId { get; private set; }
    public Guid? RelatedAtmacaCardId { get; private set; }

    private FixtureMatchEvent() { }

    internal FixtureMatchEvent(
        Guid id,
        FixtureMatchEventType type,
        int minute,
        Guid? atmacaCardId,
        Guid? relatedAtmacaCardId) : base(id)
    {
        Type = type;
        Minute = minute;
        AtmacaCardId = atmacaCardId;
        RelatedAtmacaCardId = relatedAtmacaCardId;
    }
}
