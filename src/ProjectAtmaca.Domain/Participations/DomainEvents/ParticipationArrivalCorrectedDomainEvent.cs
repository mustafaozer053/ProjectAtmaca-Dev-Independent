using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations.DomainEvents;

public sealed class ParticipationArrivalCorrectedDomainEvent
    : DomainEvent
{
    public ParticipationId ParticipationId { get; }

    public DateTimeOffset PreviousJoinedAt { get; }

    public DateTimeOffset CorrectedJoinedAt { get; }

    public ParticipationCorrectionReason Reason { get; }

    public ParticipationArrivalCorrectedDomainEvent(
        ParticipationId participationId,
        DateTimeOffset previousJoinedAt,
        DateTimeOffset correctedJoinedAt,
        ParticipationCorrectionReason reason)
    {
        ParticipationId = participationId;
        PreviousJoinedAt = previousJoinedAt;
        CorrectedJoinedAt = correctedJoinedAt;
        Reason = reason;
    }
}
