namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class Location : ValueObject
{
    public Country Country { get; }

    public string City { get; }

    public string? District { get; }

    private Location(
        Country country,
        string city,
        string? district)
    {
        Country = country;
        City = city;
        District = district;
    }

    public static Location Create(
        Country country,
        string city,
        string? district = null)
    {
        if (country is null)
            throw new ArgumentException("Country is required.");

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.");

        return new Location(
            country,
            city.Trim(),
            string.IsNullOrWhiteSpace(district)
                ? null
                : district.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Country;
        yield return City;
        yield return District;
    }

    public override string ToString()
    {
        return District is null
            ? $"{City}, {Country.Name}"
            : $"{District}, {City}, {Country.Name}";
    }
}
