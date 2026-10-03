using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Persons;

public sealed class Person : AuditableAggregateRoot
{
    private readonly List<PersonProfessionalTitle> _professionalTitles = [];

    public PersonName Name { get; private set; }

    public Country BirthCountry { get; private set; }

    public BirthDate BirthDate { get; private set; }

    public Location? BirthPlace { get; private set; }

    public PersonName? MotherName { get; private set; }

    public PersonName? FatherName { get; private set; }

    public BloodType BloodType { get; private set; }

    public Email? Email { get; private set; }

    public PhoneNumber? PrimaryPhoneNumber { get; private set; }

    public PhoneNumber? SecondaryPhoneNumber { get; private set; }

    public Address? Address { get; private set; }

    public IReadOnlyCollection<PersonProfessionalTitle> ProfessionalTitles =>
        _professionalTitles.AsReadOnly();

    private Person(
        PersonName name,
        BirthDate birthDate,
        Country birthCountry,
        Location? birthPlace,
        PersonName? motherName,
        PersonName? fatherName,
        BloodType bloodType,
        Email? email,
        PhoneNumber? primaryPhoneNumber,
        PhoneNumber? secondaryPhoneNumber,
        Address? address)
    {
        Name = name;
        BirthCountry = birthCountry;
        BirthDate = birthDate;
        BirthPlace = birthPlace;
        MotherName = motherName;
        FatherName = fatherName;
        BloodType = bloodType;
        Email = email;
        PrimaryPhoneNumber = primaryPhoneNumber;
        SecondaryPhoneNumber = secondaryPhoneNumber;
        Address = address;
    }

    public static Result<Person> Create(
        PersonName name,
        BirthDate birthDate,
        Country birthCountry,
        Location? birthPlace = null,
        PersonName? motherName = null,
        PersonName? fatherName = null,
        BloodType bloodType = BloodType.Unknown,
        Email? email = null,
        PhoneNumber? primaryPhoneNumber = null,
        PhoneNumber? secondaryPhoneNumber = null,
        Address? address = null)
    {
        if (name is null)
            return Result<Person>.Failure(Error.Create("PERSON_NAME_REQUIRED", "Person name is required."));

        if (birthCountry is null)
            return Result<Person>.Failure(Error.Create("PERSON_BIRTH_COUNTRY_REQUIRED", "Birth country is required."));

        if (birthDate is null)
            return Result<Person>.Failure(Error.Create("PERSON_BIRTH_DATE_REQUIRED", "Birth date is required."));

        if (birthPlace is not null && !birthPlace.Country.Equals(birthCountry))
            return Result<Person>.Failure(Error.Create("PERSON_BIRTH_PLACE_COUNTRY_MISMATCH", "Birth place must belong to the birth country."));

        var person = new Person(
            name,
            birthDate,
            birthCountry,
            birthPlace,
            motherName,
            fatherName,
            bloodType,
            email,
            primaryPhoneNumber,
            secondaryPhoneNumber,
            address);

        return Result<Person>.Success(person);
    }

    public void ChangeName(PersonName name)
    {
        if (name is null)
            throw new ArgumentException("Person name cannot be null.");

        Name = name;
    }

    public void ChangeBirthPlace(Location birthPlace)
    {
        if (birthPlace is null)
            throw new ArgumentException("Birth place cannot be null.");

        if (!birthPlace.Country.Equals(BirthCountry))
            throw new ArgumentException("Birth place must belong to the birth country.", nameof(birthPlace));

        BirthPlace = birthPlace;
    }

    public void ChangeMotherName(PersonName motherName)
    {
        if (motherName is null)
            throw new ArgumentException("Mother name cannot be null.");

        MotherName = motherName;
    }

    public void ChangeFatherName(PersonName fatherName)
    {
        if (fatherName is null)
            throw new ArgumentException("Father name cannot be null.");

        FatherName = fatherName;
    }

    public void ChangeBloodType(BloodType bloodType)
    {
        BloodType = bloodType;
    }

    public void ChangeEmail(Email? email)
    {
        Email = email;
    }

    public Result<PersonProfessionalTitle> AddProfessionalTitle(
        string title,
        DateOnly startedOn)
    {
        var titleResult = PersonProfessionalTitle.Create(Id, title, startedOn);
        if (titleResult.IsFailure)
            return titleResult;

        PersonProfessionalTitle newTitle = titleResult.Value!;
        bool overlapsExisting = _professionalTitles.Any(existing =>
            string.Equals(existing.Title, newTitle.Title, StringComparison.OrdinalIgnoreCase) &&
            existing.StartedOn <= (newTitle.EndedOn ?? DateOnly.MaxValue) &&
            newTitle.StartedOn <= (existing.EndedOn ?? DateOnly.MaxValue));
        if (overlapsExisting)
            return Result<PersonProfessionalTitle>.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_DUPLICATE",
                "The same professional title already exists for an overlapping period."));

        _professionalTitles.Add(newTitle);
        return Result<PersonProfessionalTitle>.Success(newTitle);
    }

    public Result EndProfessionalTitle(Guid titleId, DateOnly endedOn)
    {
        PersonProfessionalTitle? title = _professionalTitles.SingleOrDefault(
            item => item.Id == titleId);
        if (title is null)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_NOT_FOUND",
                "Professional title was not found."));

        return title.End(endedOn);
    }

    public Result UpdateProfessionalTitle(
        Guid titleId,
        string title,
        DateOnly startedOn)
    {
        PersonProfessionalTitle? professionalTitle = _professionalTitles.SingleOrDefault(
            item => item.Id == titleId);
        if (professionalTitle is null)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_NOT_FOUND",
                "Professional title was not found."));
        if (professionalTitle.EndedOn.HasValue)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_ALREADY_ENDED",
                "A professional title in history cannot be edited."));

        var updatedTitle = PersonProfessionalTitle.Create(Id, title, startedOn);
        if (updatedTitle.IsFailure)
            return Result.Failure(updatedTitle.Error!);

        bool overlapsExisting = _professionalTitles.Any(existing =>
            existing.Id != titleId &&
            string.Equals(existing.Title, updatedTitle.Value!.Title, StringComparison.OrdinalIgnoreCase) &&
            existing.StartedOn <= (existing.EndedOn ?? DateOnly.MaxValue) &&
            updatedTitle.Value.StartedOn <= (existing.EndedOn ?? DateOnly.MaxValue));
        if (overlapsExisting)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_DUPLICATE",
                "The same professional title already exists for an overlapping period."));

        return professionalTitle.Update(title, startedOn);
    }

    public Result RemoveProfessionalTitle(Guid titleId)
    {
        PersonProfessionalTitle? professionalTitle = _professionalTitles.SingleOrDefault(
            item => item.Id == titleId);
        if (professionalTitle is null)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_NOT_FOUND",
                "Professional title was not found."));
        if (professionalTitle.EndedOn.HasValue)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_ALREADY_ENDED",
                "A professional title in history cannot be deleted."));
        if (professionalTitle.EvidenceDocuments.Count > 0)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_HAS_EVIDENCE",
                "A professional title with linked evidence documents cannot be deleted."));

        _professionalTitles.Remove(professionalTitle);
        return Result.Success();
    }

    public void ChangePrimaryPhoneNumber(PhoneNumber? phoneNumber)
    {
        PrimaryPhoneNumber = phoneNumber;
    }

    public void ChangeSecondaryPhoneNumber(PhoneNumber? phoneNumber)
    {
        SecondaryPhoneNumber = phoneNumber;
    }

    public void ChangeAddress(Address? address)
    {
        Address = address;
    }
}