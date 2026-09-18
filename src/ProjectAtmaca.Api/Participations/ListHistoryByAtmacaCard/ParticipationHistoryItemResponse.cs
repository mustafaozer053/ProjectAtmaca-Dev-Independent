namespace ProjectAtmaca.Api.Participations
    .ListHistoryByAtmacaCard;

public sealed record ParticipationHistoryItemResponse(
    Guid ParticipationId,
    string ActivityTypeCode,
    Guid ActivityId,
    string StatusCode,
    string? ConditionCode,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LeftAt,
    DateTime CreatedAtUtc);
