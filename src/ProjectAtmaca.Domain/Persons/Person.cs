using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Persons;

public sealed class Person : AuditableAggregateRoot
{
    public PersonName Name { get; private set; }

    public IdentityNumber IdentityNumber { get; private set; }

    public Country Nationality { get; private set; }

    public BirthDate BirthDate { get; private set; }

    public Location BirthPlace { get; private set; }

    public PersonName MotherName { get; private set; }

    public PersonName FatherName { get; private set; }

    public BloodType BloodType { get; private set; }

    public Email? Email { get; private set; }

    public PhoneNumber? PrimaryPhoneNumber { get; private set; }

    public PhoneNumber? SecondaryPhoneNumber { get; private set; }

    public Address? Address { get; private set; }

    private Person(
        PersonName name,
        IdentityNumber identityNumber,
        Country nationality,
        BirthDate birthDate,
        Location birthPlace,
        PersonName motherName,
        PersonName fatherName,
        BloodType bloodType,
        Email? email,
        PhoneNumber? primaryPhoneNumber,
        PhoneNumber? secondaryPhoneNumber,
        Address? address)
    {
        Name = name;
        IdentityNumber = identityNumber;
        Nationality = nationality;
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
        IdentityNumber identityNumber,
        Country nationality,
        BirthDate birthDate,
        Location birthPlace,
        PersonName motherName,
        PersonName fatherName,
        BloodType bloodType = BloodType.Unknown,
        Email? email = null,
        PhoneNumber? primaryPhoneNumber = null,
        PhoneNumber? secondaryPhoneNumber = null,
        Address? address = null)
    {
        if (name is null)
            return Result<Person>.Failure(Error.Create("PERSON_NAME_REQUIRED", "Person name is required."));

        if (identityNumber is null)
            return Result<Person>.Failure(Error.Create("PERSON_IDENTITY_REQUIRED", "Identity number is required."));

        if (nationality is null)
            return Result<Person>.Failure(Error.Create("PERSON_NATIONALITY_REQUIRED", "Nationality is required."));

        if (birthDate is null)
            return Result<Person>.Failure(Error.Create("PERSON_BIRTH_DATE_REQUIRED", "Birth date is required."));

        if (birthPlace is null)
            return Result<Person>.Failure(Error.Create("PERSON_BIRTH_PLACE_REQUIRED", "Birth place is required."));

        if (motherName is null)
            return Result<Person>.Failure(Error.Create("PERSON_MOTHER_NAME_REQUIRED", "Mother name is required."));

        if (fatherName is null)
            return Result<Person>.Failure(Error.Create("PERSON_FATHER_NAME_REQUIRED", "Father name is required."));

        var person = new Person(
            name,
            identityNumber,
            nationality,
            birthDate,
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

    public void ChangeNationality(Country nationality)
    {
        if (nationality is null)
            throw new ArgumentException("Nationality cannot be null.");

        Nationality = nationality;
    }

    public void ChangeBirthPlace(Location birthPlace)
    {
        if (birthPlace is null)
            throw new ArgumentException("Birth place cannot be null.");

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