using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Domain.Participations;

public sealed class ActivityReference : ValueObject
{
    public ActivityTypeCode ActivityType { get; }

    public Guid ActivityId { get; }

    private ActivityReference()
    {
        ActivityType = null!;
    }

    private ActivityReference(
        ActivityTypeCode activityType,
        Guid activityId)
    {
        ActivityType = activityType;
        ActivityId = activityId;
    }

    public static ActivityReference ForTraining(
        TrainingId trainingId)
    {
        if (trainingId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Training id cannot be empty.",
                nameof(trainingId));
        }

        return new ActivityReference(
            ActivityTypeCode.Training,
            trainingId.Value);
    }

    public bool RefersToTraining(
        TrainingId trainingId)
    {
        return
            ActivityType == ActivityTypeCode.Training &&
            ActivityId == trainingId.Value;
    }

    protected override IEnumerable<object?>
        GetEqualityComponents()
    {
        yield return ActivityType;
        yield return ActivityId;
    }

    public override string ToString()
    {
        return $"{ActivityType}:{ActivityId}";
    }
}
