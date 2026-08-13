using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations;

public sealed class ActivityTypeCode : ValueObject
{
    public const string TrainingCode = "TRAINING";

    public static ActivityTypeCode Training { get; } =
        new(TrainingCode);

    public string Value { get; }

    private ActivityTypeCode()
    {
        Value = null!;
    }

    private ActivityTypeCode(
        string value)
    {
        Value = value;
    }

    public static Result<ActivityTypeCode> Create(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ActivityTypeCode>.Failure(
                ParticipationErrors.ActivityTypeCodeRequired);
        }

        string normalizedValue =
            value
                .Trim()
                .ToUpperInvariant();

        return normalizedValue switch
        {
            TrainingCode =>
                Result<ActivityTypeCode>.Success(
                    Training),

            _ =>
                Result<ActivityTypeCode>.Failure(
                    ParticipationErrors
                        .UnsupportedActivityTypeCode(
                            normalizedValue))
        };
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
