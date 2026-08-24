using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.ListByActivity;

public sealed record ParticipationListItem(
    Guid Id,
    Guid AtmacaCardId,
    ParticipationStatus Status,
    string? ConditionCode,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LeftAt);
