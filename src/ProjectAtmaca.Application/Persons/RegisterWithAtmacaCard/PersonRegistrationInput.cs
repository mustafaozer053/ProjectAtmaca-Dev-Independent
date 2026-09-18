using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

public sealed record PersonRegistrationInput(
    PersonName Name,
    BirthDate BirthDate,
    Country BirthCountry,
    TurkishCitizenshipStatus Status,
    string? NationalIdentityNumber = null,
    string? PassportNumber = null,
    DateOnly? TurkishCitizenshipAcquiredOn = null)
{
    private readonly IReadOnlyList<CitizenshipInput> _citizenships = Array.Empty<CitizenshipInput>();
    public IReadOnlyList<CitizenshipInput> Citizenships
    {
        get => _citizenships;
        init => _citizenships = value is null || value.Count == 0
            ? Array.Empty<CitizenshipInput>() : Array.AsReadOnly(value.ToArray());
    }
    public bool ConfirmPossiblePassportDuplicate { get; init; }
    public string? PassportDuplicateReason { get; init; }
    public Location? BirthPlace { get; init; }
    public PersonName? MotherName { get; init; }
    public PersonName? FatherName { get; init; }
    public BloodType BloodType { get; init; } = BloodType.Unknown;
    public Email? Email { get; init; }
    public PhoneNumber? PrimaryPhoneNumber { get; init; }
    public PhoneNumber? SecondaryPhoneNumber { get; init; }
    public Address? Address { get; init; }

    // Do not expose identity numbers through an automatically generated record string.
    public override string ToString() => nameof(PersonRegistrationInput);
}
