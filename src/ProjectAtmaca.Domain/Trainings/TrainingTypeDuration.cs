using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class TrainingTypeDuration : ValueObject
{
    public const int MinimumMinutes = 1;
    public const int MaximumMinutes =
        TrainingSchedule.MaximumDurationMinutes;

    public int Minutes { get; }

    private TrainingTypeDuration(int minutes)
    {
        Minutes = minutes;
    }

    public static Result<TrainingTypeDuration> Create(
        int minutes)
    {
        if (minutes < MinimumMinutes)
        {
            return Result<TrainingTypeDuration>.Failure(
                TrainingErrors.TrainingTypeDurationTooShort);
        }

        if (minutes > MaximumMinutes)
        {
            return Result<TrainingTypeDuration>.Failure(
                TrainingErrors.TrainingTypeDurationTooLong(
                    MaximumMinutes));
        }

        return Result<TrainingTypeDuration>.Success(
            new TrainingTypeDuration(minutes));
    }

    protected override IEnumerable<object?>
        GetEqualityComponents()
    {
        yield return Minutes;
    }

    public override string ToString()
    {
        return $"{Minutes} min";
    }
}
