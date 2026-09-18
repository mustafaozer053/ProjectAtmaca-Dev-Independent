using System.Text.Json;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

// Explicit versioned persistence DTOs, never serialized Domain objects or log payloads.
public sealed record CountryData(string Code, string Name)
{
    public static CountryData From(Country value) => new(value.Code, value.Name);
    public Country ToDomain() => Country.Create(Code, Name);
}
public sealed record LocationData(CountryData Country, string City, string? District)
{
    public static LocationData From(Location value) => new(CountryData.From(value.Country), value.City, value.District);
    public Location ToDomain() => Location.Create(Country.ToDomain(), City, District);
}
public sealed record AddressData(LocationData Location, string Text, string? PostalCode)
{
    public static AddressData From(Address value) => new(LocationData.From(value.Location), value.AddressText, value.PostalCode);
    public Address ToDomain() => Address.Create(Location.ToDomain(), Text, PostalCode);
}
public sealed record PhoneData(string CountryCode, string Number)
{
    public static PhoneData From(PhoneNumber value) => new(value.CountryCode, value.NationalNumber);
    public PhoneNumber ToDomain() => PhoneNumber.Create(CountryCode, Number);
}
public sealed record IdentityData(TurkishCitizenshipStatus Status, string? NationalId, string? Passport, DateOnly? AcquiredOn)
{
    public static IdentityData From(RegistrationIdentity value) => new(value.Status, value.NationalIdentityNumber,
        value.PassportNumber, value.TurkishCitizenshipAcquiredOn);
    public RegistrationIdentity ToDomain()
    {
        var result = RegistrationIdentity.Create(Status, NationalId, Passport, AcquiredOn);
        return result.IsSuccess ? result.Value! : throw new InvalidOperationException("Invalid stored registration identity.");
    }
}
public sealed record CitizenshipData(CountryData Country, DateOnly? AcquiredOn);
public sealed record RegistrationData(int Version, string Name, DateTime BirthDate, CountryData BirthCountry,
    IdentityData Identity, LocationData? BirthPlace, string? MotherName, string? FatherName, BloodType BloodType,
    string? Email, PhoneData? PrimaryPhone, PhoneData? SecondaryPhone, AddressData? Address,
    bool ConfirmPossiblePassportDuplicate = false, string? PassportDuplicateReason = null,
    CitizenshipData[]? Citizenships = null)
{
    public static RegistrationData From(PersonRegistrationInput value) => new(1, value.Name.FullName,
        value.BirthDate.Value, CountryData.From(value.BirthCountry), new(value.Status, value.NationalIdentityNumber,
            value.PassportNumber, value.TurkishCitizenshipAcquiredOn),
        value.BirthPlace is null ? null : LocationData.From(value.BirthPlace), value.MotherName?.FullName,
        value.FatherName?.FullName, value.BloodType, value.Email?.Value,
        value.PrimaryPhoneNumber is null ? null : PhoneData.From(value.PrimaryPhoneNumber),
        value.SecondaryPhoneNumber is null ? null : PhoneData.From(value.SecondaryPhoneNumber),
        value.Address is null ? null : AddressData.From(value.Address), value.ConfirmPossiblePassportDuplicate, value.PassportDuplicateReason,
        value.Citizenships.Select(x => new CitizenshipData(CountryData.From(x.Country), x.AcquiredOn)).ToArray());

    public PersonRegistrationInput ToDomain()
    {
        if (Version != 1) throw new InvalidOperationException("Unsupported registration data version.");
        return new(NameValue(Name), ProjectAtmaca.Domain.Common.ValueObjects.BirthDate.Create(BirthDate),
            BirthCountry.ToDomain(), Identity.Status, Identity.NationalId, Identity.Passport, Identity.AcquiredOn)
        {
            BirthPlace = BirthPlace?.ToDomain(), MotherName = MotherName is null ? null : NameValue(MotherName),
            FatherName = FatherName is null ? null : NameValue(FatherName), BloodType = BloodType,
            Email = Email is null ? null : ProjectAtmaca.Domain.Common.ValueObjects.Email.Create(Email),
            PrimaryPhoneNumber = PrimaryPhone?.ToDomain(), SecondaryPhoneNumber = SecondaryPhone?.ToDomain(),
            Address = Address?.ToDomain(), ConfirmPossiblePassportDuplicate = ConfirmPossiblePassportDuplicate,
            PassportDuplicateReason = PassportDuplicateReason,
            Citizenships = (Citizenships ?? []).Select(x => new CitizenshipInput(x.Country.ToDomain(), x.AcquiredOn)).ToArray()
        };
    }
    private static PersonName NameValue(string name) => PersonName.Create(name).Value
        ?? throw new InvalidOperationException("Invalid stored person name.");
}
internal static class RegistrationJson
{
    public static string Write<T>(T value) => JsonSerializer.Serialize(value);
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value)
        ?? throw new InvalidOperationException("Missing registration data.");
}
