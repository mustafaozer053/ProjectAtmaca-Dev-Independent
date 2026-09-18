using System.Globalization;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Api.Persons;

public sealed class RegisterPersonRequest
{
    public string? OperationId { get; init; }
    public string? FullName { get; init; }
    public string? BirthDate { get; init; }
    public CountryRequest? BirthCountry { get; init; }
    public int Status { get; init; }
    public string? NationalIdentityNumber { get; init; }
    public string? PassportNumber { get; init; }
    public string? TurkishCitizenshipAcquiredOn { get; init; }
    public CitizenshipRequest?[]? Citizenships { get; init; }
    public bool ConfirmPossiblePassportDuplicate { get; init; }
    public string? PassportDuplicateReason { get; init; }
    public LocationRequest? BirthPlace { get; init; }
    public string? MotherName { get; init; }
    public string? FatherName { get; init; }
    public int BloodType { get; init; }
    public string? Email { get; init; }
    public PhoneRequest? PrimaryPhoneNumber { get; init; }
    public PhoneRequest? SecondaryPhoneNumber { get; init; }
    public AddressRequest? Address { get; init; }

    internal Result<PersonRegistrationInput> Map()
    {
        try
        {
            DateOnly birth = Date(BirthDate) ?? throw new ArgumentException();
            if (birth > DateOnly.FromDateTime(DateTime.UtcNow)
                || !Enum.IsDefined(typeof(TurkishCitizenshipStatus), Status)
                || !Enum.IsDefined(typeof(BloodType), BloodType)) throw new ArgumentException();
            return Result<PersonRegistrationInput>.Success(new PersonRegistrationInput(
                Name(FullName), Domain.Common.ValueObjects.BirthDate.Create(birth.ToDateTime(TimeOnly.MinValue)),
                CountryValue(BirthCountry), (TurkishCitizenshipStatus)Status,
                NationalIdentityNumber, PassportNumber, Date(TurkishCitizenshipAcquiredOn))
            {
                Citizenships = (Citizenships ?? []).Select(c => c is null
                    ? throw new ArgumentException()
                    : new CitizenshipInput(CountryValue(c.Country), Date(c.AcquiredOn))).ToArray(),
                ConfirmPossiblePassportDuplicate = ConfirmPossiblePassportDuplicate,
                PassportDuplicateReason = PassportDuplicateReason,
                BirthPlace = BirthPlace is null ? null : Place(BirthPlace),
                MotherName = MotherName is null ? null : Name(MotherName),
                FatherName = FatherName is null ? null : Name(FatherName),
                BloodType = (BloodType)BloodType,
                Email = Email is null ? null : Domain.Common.ValueObjects.Email.Create(Email),
                PrimaryPhoneNumber = Phone(PrimaryPhoneNumber),
                SecondaryPhoneNumber = Phone(SecondaryPhoneNumber),
                Address = Address is null ? null : Domain.Common.ValueObjects.Address.Create(
                    Place(Address.Location), Address.AddressText!, Address.PostalCode)
            });
        }
        catch (ArgumentException)
        {
            // Factory messages may contain supplied personal data. Keep the public error fixed.
            return Result<PersonRegistrationInput>.Failure(Error.Create(
                "PersonRegistration.Request.Invalid", "Registration fields are missing or invalid."));
        }
    }

    private static DateOnly? Date(string? value)
    {
        if (value is null) return null;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) || date == default) throw new ArgumentException();
        return date;
    }
    private static PersonName Name(string? value)
    {
        var result = PersonName.Create(value!);
        return result.IsSuccess ? result.Value! : throw new ArgumentException();
    }
    private static Country CountryValue(CountryRequest? value) => value is null
        ? throw new ArgumentException() : Country.Create(value.Code!, value.Name!);
    private static Location Place(LocationRequest? value) => value is null
        ? throw new ArgumentException() : Location.Create(CountryValue(value.Country), value.City!, value.District);
    private static PhoneNumber? Phone(PhoneRequest? value) => value is null
        ? null : PhoneNumber.Create(value.CountryCode!, value.NationalNumber!);
}

public sealed class CountryRequest { public string? Code { get; init; } public string? Name { get; init; } }
public sealed class CitizenshipRequest { public CountryRequest? Country { get; init; } public string? AcquiredOn { get; init; } }
public sealed class LocationRequest
{
    public CountryRequest? Country { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
}
public sealed class PhoneRequest { public string? CountryCode { get; init; } public string? NationalNumber { get; init; } }
public sealed class AddressRequest
{
    public LocationRequest? Location { get; init; }
    public string? AddressText { get; init; }
    public string? PostalCode { get; init; }
}
