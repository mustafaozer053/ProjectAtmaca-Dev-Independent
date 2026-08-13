namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class Address : ValueObject
{
    public Location Location { get; }

    public string? PostalCode { get; }

    public string AddressText { get; }

    private Address(
        Location location,
        string? postalCode,
        string addressText)
    {
        Location = location;
        PostalCode = postalCode;
        AddressText = addressText;
    }

    public static Address Create(
        Location location,
        string addressText,
        string? postalCode = null)
    {
        if (location is null)
            throw new ArgumentException("Location is required.");

        if (string.IsNullOrWhiteSpace(addressText))
            throw new ArgumentException("Address text cannot be empty.");

        return new Address(
            location,
            string.IsNullOrWhiteSpace(postalCode)
                ? null
                : postalCode.Trim(),
            addressText.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Location;
        yield return PostalCode;
        yield return AddressText;
    }

    public override string ToString()
    {
        return $"{AddressText}, {Location}";
    }
}
