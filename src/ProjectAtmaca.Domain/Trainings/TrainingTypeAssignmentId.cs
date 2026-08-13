namespace ProjectAtmaca.Domain.Trainings;

public readonly record struct TrainingTypeAssignmentId
{
    public Guid Value { get; }

    private TrainingTypeAssignmentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Training type assignment id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static TrainingTypeAssignmentId New()
    {
        return new TrainingTypeAssignmentId(Guid.NewGuid());
    }

    public static TrainingTypeAssignmentId From(Guid value)
    {
        return new TrainingTypeAssignmentId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
