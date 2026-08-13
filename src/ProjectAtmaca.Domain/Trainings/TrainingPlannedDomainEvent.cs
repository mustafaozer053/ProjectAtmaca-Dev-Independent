using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings.DomainEvents;

public sealed class TrainingPlannedDomainEvent : DomainEvent
{
    public TrainingId TrainingId { get; }

    public TrainingPlannedDomainEvent(
        TrainingId trainingId)
    {
        TrainingId = trainingId;
    }
}
