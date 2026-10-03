using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.AtmacaCards;

public sealed class AtmacaCardDocument : Entity
{
    public Guid AtmacaCardId { get; private set; }
    public AtmacaCardDocumentType DocumentType { get; private set; }
    public string Title { get; private set; }
    public string? Issuer { get; private set; }
    public DateOnly? IssuedOn { get; private set; }
    public string OriginalFileName { get; private set; }
    public string ContentType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string StorageKey { get; private set; }

    private AtmacaCardDocument()
    {
        Title = null!;
        OriginalFileName = null!;
        ContentType = null!;
        StorageKey = null!;
    }

    private AtmacaCardDocument(
        Guid id,
        Guid atmacaCardId,
        AtmacaCardDocumentType documentType,
        string title,
        string? issuer,
        DateOnly? issuedOn,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string storageKey)
        : base(id)
    {
        AtmacaCardId = atmacaCardId;
        DocumentType = documentType;
        Title = title;
        Issuer = issuer;
        IssuedOn = issuedOn;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        StorageKey = storageKey;
    }

    internal static Result<AtmacaCardDocument> Create(
        Guid atmacaCardId,
        AtmacaCardDocumentType documentType,
        string title,
        string? issuer,
        DateOnly? issuedOn,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string storageKey)
    {
        if (atmacaCardId == Guid.Empty)
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_CARD_REQUIRED", "AtmacaCard id is required."));

        if (!Enum.IsDefined(documentType))
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_TYPE_INVALID", "Document type is invalid."));

        string normalizedTitle = title?.Trim() ?? string.Empty;
        if (normalizedTitle.Length is < 2 or > 150)
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_TITLE_INVALID", "Document title must be between 2 and 150 characters."));

        string? normalizedIssuer = string.IsNullOrWhiteSpace(issuer) ? null : issuer.Trim();
        if (normalizedIssuer is { Length: > 150 })
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_ISSUER_INVALID", "Document issuer cannot exceed 150 characters."));

        if (issuedOn == default)
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_ISSUED_ON_INVALID", "Document issue date is invalid."));

        string normalizedFileName = Path.GetFileName(originalFileName?.Trim() ?? string.Empty);
        if (normalizedFileName.Length is < 1 or > 255)
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_FILE_NAME_INVALID", "Original file name must not exceed 255 characters."));

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 100)
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_CONTENT_TYPE_INVALID", "File content type is invalid."));

        if (fileSizeBytes is < 1 or > 10_485_760)
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_FILE_SIZE_INVALID", "File size must be between 1 byte and 10 MB."));

        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Length > 200 ||
            Path.IsPathRooted(storageKey) || storageKey.Contains("..", StringComparison.Ordinal))
            return Result<AtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_STORAGE_KEY_INVALID", "Storage key is invalid."));

        return Result<AtmacaCardDocument>.Success(new AtmacaCardDocument(
            Guid.NewGuid(),
            atmacaCardId,
            documentType,
            normalizedTitle,
            normalizedIssuer,
            issuedOn,
            normalizedFileName,
            contentType,
            fileSizeBytes,
            storageKey));
    }
}
