using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations;

public sealed class ParticipationCorrectionReason : ValueObject
{
    public const int MaxLength = 250;

    public string Value { get; }

    private ParticipationCorrectionReason()
    {
        Value = null!;
    }

    private ParticipationCorrectionReason(
        string value)
    {
        Value = value;
    }

    public static Result<ParticipationCorrectionReason> Create(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ParticipationCorrectionReason>.Failure(
                ParticipationErrors.CorrectionReasonRequired);
        }

        string normalizedValue =
            value.Trim();

        if (normalizedValue.Length > MaxLength)
        {
            return Result<ParticipationCorrectionReason>.Failure(
                ParticipationErrors.CorrectionReasonTooLong);
        }

        return Result<ParticipationCorrectionReason>.Success(
            new ParticipationCorrectionReason(
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
