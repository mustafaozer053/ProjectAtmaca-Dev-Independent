using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings.DomainEvents;

public sealed class TrainingCancelledDomainEvent : DomainEvent
{
    public TrainingId TrainingId { get; }

    public TrainingCancelledDomainEvent(
        TrainingId trainingId)
    {
        TrainingId = trainingId;
    }
}
