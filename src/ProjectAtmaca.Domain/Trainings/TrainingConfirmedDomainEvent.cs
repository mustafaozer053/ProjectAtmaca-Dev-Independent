using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings.DomainEvents;

public sealed class TrainingConfirmedDomainEvent : DomainEvent
{
    public TrainingId TrainingId { get; }

    public TrainingConfirmedDomainEvent(
        TrainingId trainingId)
    {
        TrainingId = trainingId;
    }
}
