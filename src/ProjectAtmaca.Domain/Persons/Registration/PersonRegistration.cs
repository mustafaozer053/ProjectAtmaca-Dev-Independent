using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons.Registration;

/// <summary>
/// Identity information supplied at initial registration, retained as historical input.
/// This is neither a verified identity document nor the person's current citizenship profile.
/// </summary>
public sealed class PersonRegistration : AuditableAggregateRoot
{
    public Guid PersonId { get; private set; }
    public RegistrationIdentity Identity { get; private set; }

    private PersonRegistration(Guid personId, RegistrationIdentity identity)
    {
        PersonId = personId;
        Identity = identity;
    }

    public static Result<PersonRegistration> Record(Guid personId, RegistrationIdentity identity)
    {
        if (personId == Guid.Empty)
            return Result<PersonRegistration>.Failure(PersonRegistrationErrors.PersonRequired);

        if (identity is null)
            return Result<PersonRegistration>.Failure(PersonRegistrationErrors.IdentityRequired);

        return Result<PersonRegistration>.Success(new PersonRegistration(personId, identity));
    }
}
