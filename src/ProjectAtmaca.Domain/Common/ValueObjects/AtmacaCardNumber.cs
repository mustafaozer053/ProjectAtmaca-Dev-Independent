using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class AtmacaCardNumber : ValueObject
{
    private const string Prefix = "ATM-";
    private const int NumericPartLength = 6;

    public string Value { get; }

    private AtmacaCardNumber(string value)
    {
        Value = value;
    }

    public static Result<AtmacaCardNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<AtmacaCardNumber>.Failure(
                Error.Create(
                    "ATMACA_CARD_NUMBER_REQUIRED",
                    "AtmacaCard number is required."));
        }

        value = value.Trim().ToUpperInvariant();

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return Result<AtmacaCardNumber>.Failure(
                Error.Create(
                    "ATMACA_CARD_NUMBER_INVALID_PREFIX",
                    "AtmacaCard number must start with ATM-."));
        }

        var numericPart = value[Prefix.Length..];

        if (numericPart.Length != NumericPartLength ||
            !numericPart.All(char.IsDigit))
        {
            return Result<AtmacaCardNumber>.Failure(
                Error.Create(
                    "ATMACA_CARD_NUMBER_INVALID_FORMAT",
                    "AtmacaCard number must follow the ATM-000001 format."));
        }

        if (!int.TryParse(numericPart, out var sequenceNumber) ||
            sequenceNumber <= 0)
        {
            return Result<AtmacaCardNumber>.Failure(
                Error.Create(
                    "ATMACA_CARD_NUMBER_INVALID_SEQUENCE",
                    "AtmacaCard sequence number must be greater than zero."));
        }

        return Result<AtmacaCardNumber>.Success(
            new AtmacaCardNumber(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
