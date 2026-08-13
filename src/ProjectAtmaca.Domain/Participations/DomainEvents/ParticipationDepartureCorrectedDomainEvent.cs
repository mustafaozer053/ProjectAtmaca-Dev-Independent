using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations.DomainEvents;

public sealed class ParticipationDepartureCorrectedDomainEvent
    : DomainEvent
{
    public ParticipationId ParticipationId { get; }

    public DateTimeOffset PreviousLeftAt { get; }

    public DateTimeOffset CorrectedLeftAt { get; }

    public ParticipationCorrectionReason Reason { get; }

    public ParticipationDepartureCorrectedDomainEvent(
        ParticipationId participationId,
        DateTimeOffset previousLeftAt,
        DateTimeOffset correctedLeftAt,
        ParticipationCorrectionReason reason)
    {
        ParticipationId = participationId;
        PreviousLeftAt = previousLeftAt;
        CorrectedLeftAt = correctedLeftAt;
        Reason = reason;
    }
}
