using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.GetById;

public sealed record ParticipationDetails(
    Guid Id,
    Guid AtmacaCardId,
    string ActivityTypeCode,
    Guid ActivityId,
    ParticipationStatus Status,
    string? ConditionCode,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LeftAt,
    string? Note);
