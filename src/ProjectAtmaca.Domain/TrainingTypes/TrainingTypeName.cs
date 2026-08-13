using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.TrainingTypes;

public sealed class TrainingTypeName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    private TrainingTypeName(string value)
    {
        Value = value;
    }

    public static Result<TrainingTypeName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<TrainingTypeName>.Failure(
                TrainingTypeErrors.NameEmpty);
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > MaxLength)
        {
            return Result<TrainingTypeName>.Failure(
                TrainingTypeErrors.NameTooLong(MaxLength));
        }

        return Result<TrainingTypeName>.Success(
            new TrainingTypeName(normalizedValue));
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
