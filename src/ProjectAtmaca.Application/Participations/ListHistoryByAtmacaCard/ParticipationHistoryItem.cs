using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.ListHistoryByAtmacaCard;

public sealed record ParticipationHistoryItem(
    Guid ParticipationId,
    ActivityReference ActivityReference,
    ParticipationStatus Status,
    string? ConditionCode,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LeftAt,
    DateTime CreatedAtUtc);
