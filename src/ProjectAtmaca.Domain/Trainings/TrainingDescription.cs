using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class TrainingDescription : ValueObject
{
    public const int MaxLength = 1000;

    public string Value { get; }

    private TrainingDescription(string value)
    {
        Value = value;
    }

    public static Result<TrainingDescription> Create(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;

        if (normalizedValue.Length > MaxLength)
        {
            return Result<TrainingDescription>.Failure(
                TrainingErrors.DescriptionTooLong(MaxLength));
        }

        return Result<TrainingDescription>.Success(
            new TrainingDescription(normalizedValue));
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
