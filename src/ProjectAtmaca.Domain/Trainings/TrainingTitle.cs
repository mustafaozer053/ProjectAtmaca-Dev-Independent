using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class TrainingTitle : ValueObject
{
    public const int MaxLength = 150;

    public string Value { get; }

    private TrainingTitle(string value)
    {
        Value = value;
    }

    public static Result<TrainingTitle> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<TrainingTitle>.Failure(
                TrainingErrors.TitleEmpty);
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > MaxLength)
        {
            return Result<TrainingTitle>.Failure(
                TrainingErrors.TitleTooLong(MaxLength));
        }

        return Result<TrainingTitle>.Success(
            new TrainingTitle(normalizedValue));
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
