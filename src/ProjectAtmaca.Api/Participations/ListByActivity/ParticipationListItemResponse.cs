namespace ProjectAtmaca.Api.Participations.ListByActivity;

public sealed record ParticipationListItemResponse(
    Guid Id,
    Guid AtmacaCardId,
    string Status,
    string? ConditionCode,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LeftAt);
