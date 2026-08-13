using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.TrainingTypes;

public sealed class TrainingTypeCode : ValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private TrainingTypeCode(string value)
    {
        Value = value;
    }

    public static Result<TrainingTypeCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<TrainingTypeCode>.Failure(
                TrainingTypeErrors.CodeEmpty);
        }

        string normalizedValue = value
            .Trim()
            .ToUpperInvariant();

        if (normalizedValue.Length > MaxLength)
        {
            return Result<TrainingTypeCode>.Failure(
                TrainingTypeErrors.CodeTooLong(MaxLength));
        }

        if (!IsAsciiLetter(normalizedValue[0]))
        {
            return Result<TrainingTypeCode>.Failure(
                TrainingTypeErrors.CodeMustStartWithLetter);
        }

        if (normalizedValue.Any(character =>
                !IsAsciiLetter(character) &&
                !char.IsDigit(character) &&
                character != '_'))
        {
            return Result<TrainingTypeCode>.Failure(
                TrainingTypeErrors.CodeInvalidCharacters);
        }

        return Result<TrainingTypeCode>.Success(
            new TrainingTypeCode(normalizedValue));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    private static bool IsAsciiLetter(char character)
    {
        return character is >= 'A' and <= 'Z';
    }
}
