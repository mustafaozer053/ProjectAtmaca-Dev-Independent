using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations.DomainEvents;

public sealed class ParticipationCreatedDomainEvent
    : DomainEvent
{
    public ParticipationId ParticipationId { get; }

    public ActivityReference ActivityReference { get; }

    public AtmacaCardId AtmacaCardId { get; }

    public ParticipationCreatedDomainEvent(
        ParticipationId participationId,
        ActivityReference activityReference,
        AtmacaCardId atmacaCardId)
    {
        ParticipationId = participationId;
        ActivityReference = activityReference;
        AtmacaCardId = atmacaCardId;
    }
}
