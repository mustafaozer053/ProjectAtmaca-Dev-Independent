using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Fixtures;

public sealed class FixtureSquadMember : Entity
{
    public Guid AtmacaCardId { get; private set; }
    public FixtureSquadRole Role { get; private set; }

    private FixtureSquadMember() { }

    internal FixtureSquadMember(
        Guid id,
        Guid atmacaCardId,
        FixtureSquadRole role) : base(id)
    {
        AtmacaCardId = atmacaCardId;
        Role = role;
    }
}
