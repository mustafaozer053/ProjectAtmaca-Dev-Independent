using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Fixtures;

public sealed class FixtureCorrection : Entity
{
    public const int MaxReasonLength = 250;

    public string Reason { get; private set; } = null!;
    public DateTime ReopenedAtUtc { get; private set; }
    public ActorId ReopenedByActorId { get; private set; }

    private FixtureCorrection() { }

    internal FixtureCorrection(
        Guid id,
        string reason,
        DateTime reopenedAtUtc,
        ActorId reopenedByActorId) : base(id)
    {
        Reason = reason;
        ReopenedAtUtc = reopenedAtUtc;
        ReopenedByActorId = reopenedByActorId;
    }
}
