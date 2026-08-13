namespace ProjectAtmaca.Domain.Common;

public abstract class AuditableAggregateRoot : AggregateRoot
{
    public DateTime CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTime? LastModifiedAtUtc { get; private set; }

    public string? LastModifiedBy { get; private set; }

    protected AuditableAggregateRoot()
    {
        CreatedAtUtc = DateTime.UtcNow;
    }

    protected AuditableAggregateRoot(Guid id)
        : base(id)
    {
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetCreatedBy(string createdBy)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by cannot be empty.");

        CreatedBy = createdBy.Trim();
    }

    public void MarkAsModified(string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(modifiedBy))
            throw new ArgumentException("Modified by cannot be empty.");

        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy.Trim();
    }
}
