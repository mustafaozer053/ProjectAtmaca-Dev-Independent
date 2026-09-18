namespace ProjectAtmaca.Domain.Services;

public sealed class AtmacaCardNumberCapacityException : Exception
{
    public AtmacaCardNumberCapacityException(Exception innerException)
        : base("Atmaca card number capacity has been exhausted.", innerException) { }
}
