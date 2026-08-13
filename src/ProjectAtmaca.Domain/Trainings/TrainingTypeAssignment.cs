using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class TrainingTypeAssignment : Entity
{
    public TrainingTypeAssignmentId TrainingTypeAssignmentId =>
        TrainingTypeAssignmentId.From(Id);

    public TrainingTypeId TrainingTypeId { get; private set; }

    public TrainingTypeDuration Duration { get; private set; }

    private TrainingTypeAssignment()
    {
        Duration = null!;
    }

    private TrainingTypeAssignment(
        TrainingTypeAssignmentId id,
        TrainingTypeId trainingTypeId,
        TrainingTypeDuration duration)
        : base(id.Value)
    {
        TrainingTypeId = trainingTypeId;
        Duration = duration;
    }

    public static TrainingTypeAssignment Create(
        TrainingTypeId trainingTypeId,
        TrainingTypeDuration duration)
    {
        ArgumentNullException.ThrowIfNull(duration);

        return new TrainingTypeAssignment(
            TrainingTypeAssignmentId.New(),
            trainingTypeId,
            duration);
    }

    public void ChangeDuration(
        TrainingTypeDuration duration)
    {
        ArgumentNullException.ThrowIfNull(duration);

        Duration = duration;
    }
}
