using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.AtmacaCards.ManageDocuments;

public sealed record RecordAtmacaCardDocumentCommand(
    AtmacaCardId AtmacaCardId,
    AtmacaCardDocumentType DocumentType,
    string Title,
    string? Issuer,
    DateOnly? IssuedOn,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content);
