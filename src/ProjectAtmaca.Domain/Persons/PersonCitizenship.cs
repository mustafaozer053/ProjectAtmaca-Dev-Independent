using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Persons;

public sealed class PersonCitizenship : AuditableAggregateRoot
{
    public Guid PersonId { get; private set; }
    public Country Country { get; private set; }
    public DateOnly? AcquiredOn { get; private set; }

    private PersonCitizenship(Guid personId, Country country, DateOnly? acquiredOn)
    {
        PersonId = personId;
        Country = country;
        AcquiredOn = acquiredOn;
    }

    public static Result<PersonCitizenship> Register(
        Guid personId,
        Country country,
        DateOnly? acquiredOn = null)
    {
        if (personId == Guid.Empty)
            return Result<PersonCitizenship>.Failure(PersonCitizenshipErrors.PersonRequired);

        if (country is null)
            return Result<PersonCitizenship>.Failure(PersonCitizenshipErrors.CountryRequired);

        if (acquiredOn == DateOnly.MinValue)
            return Result<PersonCitizenship>.Failure(PersonCitizenshipErrors.AcquisitionDateInvalid);

        return Result<PersonCitizenship>.Success(
            new PersonCitizenship(personId, country, acquiredOn));
    }
}
