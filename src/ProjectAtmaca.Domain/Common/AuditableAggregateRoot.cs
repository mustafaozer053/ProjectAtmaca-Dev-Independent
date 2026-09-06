using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Domain.Common;

public abstract class AuditableAggregateRoot : AggregateRoot
{
    public DateTime CreatedAtUtc { get; private set; }

    public ActorId? CreatedByActorId { get; private set; }

    public DateTime? LastModifiedAtUtc { get; private set; }

    public ActorId? LastModifiedByActorId { get; private set; }

    protected AuditableAggregateRoot()
    {
        CreatedAtUtc =
            DateTime.UtcNow;
    }

    protected AuditableAggregateRoot(
        Guid id)
        : base(
            id)
    {
        CreatedAtUtc =
            DateTime.UtcNow;
    }

    public void SetCreatedBy(
        ActorId createdByActorId)
    {
        EnsureActorIdIsNotEmpty(
            createdByActorId,
            nameof(createdByActorId));

        if (CreatedByActorId.HasValue)
        {
            throw new InvalidOperationException(
                "Creation actor cannot be replaced.");
        }

        CreatedByActorId =
            createdByActorId;
    }

    public void MarkAsModified(
        ActorId modifiedByActorId)
    {
        EnsureActorIdIsNotEmpty(
            modifiedByActorId,
            nameof(modifiedByActorId));

        LastModifiedAtUtc =
            DateTime.UtcNow;

        LastModifiedByActorId =
            modifiedByActorId;
    }

    private static void EnsureActorIdIsNotEmpty(
        ActorId actorId,
        string parameterName)
    {
        if (actorId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor id cannot be empty.",
                parameterName);
        }
    }
}
