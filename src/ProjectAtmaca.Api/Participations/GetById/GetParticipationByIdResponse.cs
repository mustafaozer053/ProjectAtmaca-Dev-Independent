namespace ProjectAtmaca.Api.Participations.GetById;

public sealed record GetParticipationByIdResponse(
    Guid ParticipationId,
    Guid AtmacaCardId,
    string ActivityTypeCode,
    Guid ActivityId,
    string Status,
    string? ConditionCode,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LeftAt,
    string? Note);