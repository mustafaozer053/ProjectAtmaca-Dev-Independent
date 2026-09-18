using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Persons;

public sealed class Person : AuditableAggregateRoot
{
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