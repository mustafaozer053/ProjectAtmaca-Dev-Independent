namespace ProjectAtmaca.Api.Participations
    .ListHistoryByAtmacaCard;

public sealed record ParticipationHistoryPageResponse(
    IReadOnlyList<ParticipationHistoryItemResponse> Items,
    ParticipationHistoryCursorResponse? NextCursor);
