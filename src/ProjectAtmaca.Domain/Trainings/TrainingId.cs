namespace ProjectAtmaca.Domain.Trainings;

public readonly record struct TrainingId
{
    public Guid Value { get; }

    private TrainingId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Training id cannot be empty.",
                nameof(value));

        Value = value;
    }

    public static TrainingId New()
    {
        return new TrainingId(Guid.NewGuid());
    }

    public static TrainingId From(Guid value)
    {
        return new TrainingId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
