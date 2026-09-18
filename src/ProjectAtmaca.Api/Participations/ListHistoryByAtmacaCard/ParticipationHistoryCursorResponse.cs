namespace ProjectAtmaca.Api.Participations
    .ListHistoryByAtmacaCard;

public sealed record ParticipationHistoryCursorResponse(
    Guid AtmacaCardId,
    DateTime CreatedAtUtc,
    Guid ParticipationId);
