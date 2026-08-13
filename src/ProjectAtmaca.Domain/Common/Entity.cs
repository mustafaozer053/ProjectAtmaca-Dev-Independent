namespace ProjectAtmaca.Domain.Common;

public abstract class Entity : BaseEntity
{
    protected Entity() : base()
    {
    }

    protected Entity(Guid id) : base(id)
    {
    }
}
