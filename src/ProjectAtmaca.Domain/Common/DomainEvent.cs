namespace ProjectAtmaca.Domain.Common;

public abstract class DomainEvent
{
    public Guid Id { get; }

    public DateTime OccurredOnUtc { get; }

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }
}
