using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons;

public static class PersonIdentityDocumentErrors
{
    public static readonly Error PersonRequired = Error.Create(
        "PersonIdentityDocument.Person.Required", "Person id is required.");

    public static readonly Error IdentityNumberRequired = Error.Create(
        "PersonIdentityDocument.IdentityNumber.Required", "Document identity is required.");

    public static readonly Error DocumentTypeUnsupported = Error.Create(
        "PersonIdentityDocument.Type.Unsupported", "Only national identity cards and passports are supported.");

    public static readonly Error IssueDateRequired = Error.Create(
        "PersonIdentityDocument.IssuedOn.Required", "Document issue date is required.");

    public static readonly Error ExpiryDateRequired = Error.Create(
        "PersonIdentityDocument.ExpiresOn.Required", "Document expiry date is required.");

    public static readonly Error DateRangeInvalid = Error.Create(
        "PersonIdentityDocument.DateRange.Invalid", "Document expiry date cannot precede its issue date.");
}
