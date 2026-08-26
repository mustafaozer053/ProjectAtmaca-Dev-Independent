using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application
    .Participations.ListHistoryByAtmacaCard;

public sealed record ListParticipationHistoryByAtmacaCardQuery(
    AtmacaCardId AtmacaCardId,
    int PageSize,
    ParticipationHistoryCursor? Cursor);
