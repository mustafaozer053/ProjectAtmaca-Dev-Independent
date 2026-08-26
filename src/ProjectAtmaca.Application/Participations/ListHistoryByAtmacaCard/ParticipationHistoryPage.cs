namespace ProjectAtmaca.Application
    .Participations.ListHistoryByAtmacaCard;

public sealed record ParticipationHistoryPage(
    IReadOnlyList<ParticipationHistoryItem> Items,
    ParticipationHistoryCursor? NextCursor);
