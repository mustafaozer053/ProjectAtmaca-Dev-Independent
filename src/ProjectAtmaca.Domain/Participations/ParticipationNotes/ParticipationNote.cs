using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations;

public sealed class ParticipationNote : ValueObject
{
    public const int MaxLength = 250;

    public string Value { get; }

    private ParticipationNote()
    {
        Value = null!;
    }

    private ParticipationNote(
        string value)
    {
        Value = value;
    }

    public static Result<ParticipationNote?> Create(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ParticipationNote?>.Success(
                null);
        }

        string normalizedValue =
            value.Trim();

        if (normalizedValue.Length > MaxLength)
        {
            return Result<ParticipationNote?>.Failure(
                ParticipationErrors.NoteTooLong);
        }

        return Result<ParticipationNote?>.Success(
            new ParticipationNote(
                normalizedValue));
    }

    protected override IEnumerable<object?>
        GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
