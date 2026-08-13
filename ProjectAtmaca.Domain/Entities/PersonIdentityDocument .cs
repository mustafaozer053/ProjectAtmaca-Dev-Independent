using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Enums;

namespace ProjectAtmaca.Domain.Entities;

public sealed class PersonIdentityDocument : BaseEntity
{
    public Guid PersonId { get; private set; }

    public Guid CountryId { get; private set; }

    public IdentityDocumentType DocumentType { get; private set; }

    public string DocumentNumber { get; private set; } = string.Empty;

    public DateOnly IssueDate { get; private set; }

    public DateOnly ExpiryDate { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static PersonIdentityDocument Create(
    Guid personId,
    Guid countryId,
    IdentityDocumentType documentType,
    string documentNumber,
    DateOnly issueDate,
    DateOnly expiryDate)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("Person id is required.", nameof(personId));

        if (countryId == Guid.Empty)
            throw new ArgumentException("Country id is required.", nameof(countryId));

        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.", nameof(documentNumber));

        if (issueDate > expiryDate)
            throw new ArgumentException("Issue date cannot be later than expiry date.");

        return new PersonIdentityDocument
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CountryId = countryId,
            DocumentType = documentType,
            DocumentNumber = documentNumber.Trim(),
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            IsActive = true
        };
    }
}
