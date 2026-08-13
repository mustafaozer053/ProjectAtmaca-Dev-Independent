namespace ProjectAtmaca.Domain.Common.ValueObjects;

public enum IdentityType
{
    NationalId = 1,
    Passport = 2,
    ResidencePermit = 3,
    ForeignIdentityCard = 4
}

public sealed class IdentityNumber : ValueObject
{
    public string CountryCode { get; }

    public IdentityType IdentityType { get; }

    public string Number { get; }

    public string DisplayValue =>
        $"{CountryCode}-{IdentityType}-{Number}";

    private IdentityNumber(
        string countryCode,
        IdentityType identityType,
        string number)
    {
        CountryCode = countryCode;
        IdentityType = identityType;
        Number = number;
    }

    public static Result<IdentityNumber> Create(
        string countryCode,
        IdentityType identityType,
        string number)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return Result<IdentityNumber>.Failure(
                Error.Create(
                    "IDENTITY_COUNTRY_CODE_REQUIRED",
                    "Identity country code is required."));
        }

        if (!Enum.IsDefined(identityType))
        {
            return Result<IdentityNumber>.Failure(
                Error.Create(
                    "IDENTITY_TYPE_INVALID",
                    "Identity type is invalid."));
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            return Result<IdentityNumber>.Failure(
                Error.Create(
                    "IDENTITY_NUMBER_REQUIRED",
                    "Identity number is required."));
        }

        countryCode = countryCode
            .Trim()
            .ToUpperInvariant();

        number = number
            .Trim()
            .ToUpperInvariant();

        if (countryCode.Length != 2 ||
            !countryCode.All(char.IsLetter))
        {
            return Result<IdentityNumber>.Failure(
                Error.Create(
                    "IDENTITY_COUNTRY_CODE_INVALID",
                    "Country code must follow the ISO Alpha-2 format."));
        }

        if (number.Length < 3 || number.Length > 30)
        {
            return Result<IdentityNumber>.Failure(
                Error.Create(
                    "IDENTITY_NUMBER_LENGTH_INVALID",
                    "Identity number length is invalid."));
        }

        return Result<IdentityNumber>.Success(
            new IdentityNumber(
                countryCode,
                identityType,
                number));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return IdentityType;
        yield return Number;
    }

    public override string ToString()
    {
        return DisplayValue;
    }
}
