using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Persons;

public sealed class PersonIdentityDocument : AuditableAggregateRoot
{
    public Guid PersonId { get; private set; }
    public IdentityNumber IdentityNumber { get; private set; }
    public DateOnly IssuedOn { get; private set; }
    public DateOnly ExpiresOn { get; private set; }

    private PersonIdentityDocument(
        Guid personId,
        IdentityNumber identityNumber,
        DateOnly issuedOn,
        DateOnly expiresOn)
    {
        PersonId = personId;
        IdentityNumber = identityNumber;
        IssuedOn = issuedOn;
        ExpiresOn = expiresOn;
    }

    public static Result<PersonIdentityDocument> Register(
        Guid personId,
        IdentityNumber identityNumber,
        DateOnly issuedOn,
        DateOnly expiresOn)
    {
        if (personId == Guid.Empty)
            return Result<PersonIdentityDocument>.Failure(PersonIdentityDocumentErrors.PersonRequired);

        if (identityNumber is null)
            return Result<PersonIdentityDocument>.Failure(PersonIdentityDocumentErrors.IdentityNumberRequired);

        if (identityNumber.IdentityType is not (IdentityType.NationalId or IdentityType.Passport))
            return Result<PersonIdentityDocument>.Failure(PersonIdentityDocumentErrors.DocumentTypeUnsupported);

        if (issuedOn == default)
            return Result<PersonIdentityDocument>.Failure(PersonIdentityDocumentErrors.IssueDateRequired);

        if (expiresOn == default)
            return Result<PersonIdentityDocument>.Failure(PersonIdentityDocumentErrors.ExpiryDateRequired);

        if (expiresOn < issuedOn)
            return Result<PersonIdentityDocument>.Failure(PersonIdentityDocumentErrors.DateRangeInvalid);

        return Result<PersonIdentityDocument>.Success(
            new PersonIdentityDocument(personId, identityNumber, issuedOn, expiresOn));
    }
}
