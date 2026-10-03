namespace ProjectAtmaca.Domain.AtmacaCards;

public interface IAtmacaCardRepository
{
    Task<AtmacaCard?> GetByIdAsync(
        AtmacaCardId id,
        CancellationToken cancellationToken = default);
}
