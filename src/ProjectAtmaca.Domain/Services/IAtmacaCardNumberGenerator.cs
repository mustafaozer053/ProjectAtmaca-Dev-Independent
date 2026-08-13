namespace ProjectAtmaca.Domain.Services;

public interface IAtmacaCardNumberGenerator
{
    Task<string> GenerateAsync(
        CancellationToken cancellationToken = default);
}
