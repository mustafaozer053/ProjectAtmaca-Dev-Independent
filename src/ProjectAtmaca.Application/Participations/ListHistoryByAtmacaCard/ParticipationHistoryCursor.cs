using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application
    .Participations.ListHistoryByAtmacaCard;

public sealed record ParticipationHistoryCursor(
    AtmacaCardId AtmacaCardId,
    DateTime CreatedAtUtc,
    Guid ParticipationId);
