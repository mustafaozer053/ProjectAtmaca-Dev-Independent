using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class TrainingLocation : ValueObject
{
    public const int MaxLength = 150;

    public string Value { get; }

    private TrainingLocation(string value)
    {
        Value = value;
    }

    public static Result<TrainingLocation> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<TrainingLocation>.Failure(
                TrainingErrors.LocationEmpty);
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > MaxLength)
        {
            return Result<TrainingLocation>.Failure(
                TrainingErrors.LocationTooLong(MaxLength));
        }

        return Result<TrainingLocation>.Success(
            new TrainingLocation(normalizedValue));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
