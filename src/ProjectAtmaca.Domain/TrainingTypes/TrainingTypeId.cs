namespace ProjectAtmaca.Domain.TrainingTypes;

public readonly record struct TrainingTypeId
{
    public Guid Value { get; }

    private TrainingTypeId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Training type id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static TrainingTypeId New()
    {
        return new TrainingTypeId(Guid.NewGuid());
    }

    public static TrainingTypeId From(Guid value)
    {
        return new TrainingTypeId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
