using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.AtmacaCards.ChangeStatus;

public sealed record ChangeAtmacaCardStatusCommand(
    AtmacaCardId AtmacaCardId,
    bool IsActive);
