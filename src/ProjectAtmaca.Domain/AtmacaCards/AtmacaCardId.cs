namespace ProjectAtmaca.Domain.AtmacaCards;

public readonly record struct AtmacaCardId
{
    public Guid Value { get; }

    private AtmacaCardId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Atmaca card id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static AtmacaCardId New()
    {
        return new AtmacaCardId(Guid.NewGuid());
    }

    public static AtmacaCardId From(Guid value)
    {
        return new AtmacaCardId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
