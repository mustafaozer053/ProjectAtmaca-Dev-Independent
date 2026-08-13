namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    public string CountryCode { get; }

    public string NationalNumber { get; }

    public string FullNumber => $"+{CountryCode}{NationalNumber}";

    private PhoneNumber(string countryCode, string nationalNumber)
    {
        CountryCode = countryCode;
        NationalNumber = nationalNumber;
    }

    public static PhoneNumber Create(string countryCode, string nationalNumber)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Country code cannot be empty.");

        if (string.IsNullOrWhiteSpace(nationalNumber))
            throw new ArgumentException("National number cannot be empty.");

        countryCode = NormalizeDigits(countryCode);
        nationalNumber = NormalizeDigits(nationalNumber);

        if (countryCode.Length < 1 || countryCode.Length > 4)
            throw new ArgumentException("Country code length is invalid.");

        if (nationalNumber.Length < 6 || nationalNumber.Length > 15)
            throw new ArgumentException("National number length is invalid.");

        return new PhoneNumber(countryCode, nationalNumber);
    }

    private static string NormalizeDigits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return NationalNumber;
    }

    public override string ToString()
    {
        return FullNumber;
    }
}
