namespace ProjectAtmaca.Web.Services;

// Client-side mirrors of ProjectAtmaca.Api.Persons request/response contracts
// for POST /api/person-registrations.

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
}

public sealed class CountryRequest
{
    public string? Code { get; init; }
    public string? Name { get; init; }
}

public sealed class CitizenshipRequest
{
    public CountryRequest? Country { get; init; }
    public string? AcquiredOn { get; init; }
}

public sealed class LocationRequest
{
    public CountryRequest? Country { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
}

public sealed class PhoneRequest
{
    public string? CountryCode { get; init; }
    public string? NationalNumber { get; init; }
}

public sealed class AddressRequest
{
    public LocationRequest? Location { get; init; }
    public string? AddressText { get; init; }
    public string? PostalCode { get; init; }
}

public sealed record RegistrationReceiptResponse(
    Guid PersonId,
    Guid AtmacaCardId,
    string CardNumber,
    DateTime IssuedAtUtc);

// Mirrors the ProblemDetails shape returned by PersonRegistrationsController.ToProblem.
public sealed class PersonRegistrationProblemResponse
{
    public int Status { get; init; }
    public string? Title { get; init; }
    public string? Detail { get; init; }
    public string? Code { get; init; }
}
