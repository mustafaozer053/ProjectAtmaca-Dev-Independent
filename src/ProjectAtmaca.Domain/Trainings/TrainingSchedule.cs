using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class TrainingSchedule : ValueObject
{
    public const int MinimumDurationMinutes = 15;
    public const int MaximumDurationMinutes = 240;

    public DateOnly Date { get; }

    public TimeOnly StartTime { get; }

    public TimeOnly EndTime { get; }

    public int DurationMinutes =>
        (int)(EndTime - StartTime).TotalMinutes;

    private TrainingSchedule(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
    }

    public static Result<TrainingSchedule> Create(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            return Result<TrainingSchedule>.Failure(
                TrainingErrors.ScheduleEndMustBeAfterStart);
        }

        int durationMinutes =
            (int)(endTime - startTime).TotalMinutes;

        if (durationMinutes < MinimumDurationMinutes)
        {
            return Result<TrainingSchedule>.Failure(
                TrainingErrors.ScheduleTooShort(
                    MinimumDurationMinutes));
        }

        if (durationMinutes > MaximumDurationMinutes)
        {
            return Result<TrainingSchedule>.Failure(
                TrainingErrors.ScheduleTooLong(
                    MaximumDurationMinutes));
        }

        return Result<TrainingSchedule>.Success(
            new TrainingSchedule(
                date,
                startTime,
                endTime));
    }

    protected override IEnumerable<object?>
        GetEqualityComponents()
    {
        yield return Date;
        yield return StartTime;
        yield return EndTime;
    }

    public override string ToString()
    {
        return $"{Date:dd.MM.yyyy} " +
               $"{StartTime:HH\\:mm} - {EndTime:HH\\:mm}";
    }
}
