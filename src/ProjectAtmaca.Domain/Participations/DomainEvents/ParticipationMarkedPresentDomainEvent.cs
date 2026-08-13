using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations.DomainEvents;

public sealed class ParticipationMarkedPresentDomainEvent
    : DomainEvent
{
    public ParticipationId ParticipationId { get; }

    public ParticipationMarkedPresentDomainEvent(
        ParticipationId participationId)
    {
        ParticipationId = participationId;
    }
}
