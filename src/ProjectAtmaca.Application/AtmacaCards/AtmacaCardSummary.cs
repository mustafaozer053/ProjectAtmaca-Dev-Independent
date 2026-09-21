namespace ProjectAtmaca.Application.AtmacaCards;

public sealed record AtmacaCardSummary(
    Guid AtmacaCardId,
    Guid PersonId,
    string FullName,
    string CardNumber);
