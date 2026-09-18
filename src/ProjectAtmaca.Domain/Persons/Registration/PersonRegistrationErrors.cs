using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons.Registration;

public static class PersonRegistrationErrors
{
    public static readonly Error PersonRequired = Error.Create(
        "PersonRegistration.Person.Required", "Person id is required.");
    public static readonly Error IdentityRequired = Error.Create(
        "PersonRegistration.Identity.Required", "Registration identity is required.");
}
