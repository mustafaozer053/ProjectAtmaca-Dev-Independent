using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings;
// Reserved for future planning-based aggregates.
public sealed class PlannedDuration : ValueObject
{
    public const int MinimumMinutes = 15;
    public const int MaximumMinutes = 240;

    public int Value { get; }

    private PlannedDuration(int value)
    {
        Value = value;
    }

    public static Result<PlannedDuration> Create(int value)
    {
        if (value < MinimumMinutes)
        {
            return Result<PlannedDuration>.Failure(
                TrainingErrors.DurationTooShort);
        }

        if (value > MaximumMinutes)
        {
            return Result<PlannedDuration>.Failure(
                TrainingErrors.DurationTooLong);
        }

        return Result<PlannedDuration>.Success(
            new PlannedDuration(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return $"{Value} min";
    }
}
