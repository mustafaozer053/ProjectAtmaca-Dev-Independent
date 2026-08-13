using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.TrainingTypes;

public sealed class TrainingTypeDescription : ValueObject
{
    public const int MaxLength = 1000;

    public string Value { get; }

    private TrainingTypeDescription(string value)
    {
        Value = value;
    }

    public static Result<TrainingTypeDescription> Create(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;

        if (normalizedValue.Length > MaxLength)
        {
            return Result<TrainingTypeDescription>.Failure(
                TrainingTypeErrors.DescriptionTooLong(MaxLength));
        }

        return Result<TrainingTypeDescription>.Success(
            new TrainingTypeDescription(normalizedValue));
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
