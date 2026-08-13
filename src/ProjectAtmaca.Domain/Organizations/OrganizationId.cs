namespace ProjectAtmaca.Domain.Organizations;

public readonly record struct OrganizationId
{
    public Guid Value { get; }

    private OrganizationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static OrganizationId New()
    {
        return new OrganizationId(Guid.NewGuid());
    }

    public static OrganizationId From(Guid value)
    {
        return new OrganizationId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}