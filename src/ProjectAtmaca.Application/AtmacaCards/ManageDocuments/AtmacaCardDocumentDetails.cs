using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.AtmacaCards.ManageDocuments;

public sealed record AtmacaCardDocumentDetails(
    Guid Id,
    AtmacaCardDocumentType DocumentType,
    string Title,
    string? Issuer,
    DateOnly? IssuedOn,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes);
