using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons;

public static class PersonCitizenshipErrors
{
    public static readonly Error PersonRequired = Error.Create(
        "PersonCitizenship.Person.Required", "Person id is required.");

    public static readonly Error CountryRequired = Error.Create(
        "PersonCitizenship.Country.Required", "Citizenship country is required.");

    public static readonly Error AcquisitionDateInvalid = Error.Create(
        "PersonCitizenship.AcquiredOn.Invalid", "A supplied citizenship acquisition date must not be the default date.");
}
