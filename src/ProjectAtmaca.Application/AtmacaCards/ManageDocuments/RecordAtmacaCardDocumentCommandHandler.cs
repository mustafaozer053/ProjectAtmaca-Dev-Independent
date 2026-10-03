using Microsoft.Extensions.Logging;
using ProjectAtmaca.Application.Abstractions.Files;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageDocuments;

public sealed class RecordAtmacaCardDocumentCommandHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IAtmacaCardDocumentStorage storage,
    IUnitOfWork unitOfWork,
    ILogger<RecordAtmacaCardDocumentCommandHandler> logger)
{
    private const int MaximumFileSizeBytes = 10 * 1024 * 1024;

    public async Task<Result<AtmacaCardDocumentDetails>> Handle(
        RecordAtmacaCardDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageAtmacaCardDocuments,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<AtmacaCardDocumentDetails>.Failure(authorization.Error!);

        if (command.AtmacaCardId.Value == Guid.Empty)
            return Result<AtmacaCardDocumentDetails>.Failure(AtmacaCardApplicationErrors.InvalidId);

        if (command.FileSizeBytes is < 1 or > MaximumFileSizeBytes)
            return InvalidFileSize();

        var card = await repository.GetByIdAsync(command.AtmacaCardId, cancellationToken);
        if (card is null)
            return Result<AtmacaCardDocumentDetails>.Failure(AtmacaCardApplicationErrors.NotFound);

        var bufferedFile = await ReadAndValidateFileAsync(command, cancellationToken);
        if (bufferedFile.IsFailure)
            return Result<AtmacaCardDocumentDetails>.Failure(bufferedFile.Error!);

        string storageKey = $"atmaca-cards/{command.AtmacaCardId.Value:N}/{Guid.NewGuid():N}{bufferedFile.Value!.Extension}";
        await using var content = new MemoryStream(bufferedFile.Value.Content, writable: false);
        await storage.StoreAsync(storageKey, content, cancellationToken);

        var documentResult = card.AddDocument(
            command.DocumentType,
            command.Title,
            command.Issuer,
            command.IssuedOn,
            Path.GetFileName(command.FileName),
            bufferedFile.Value.ContentType,
            bufferedFile.Value.Content.LongLength,
            storageKey);
        if (documentResult.IsFailure)
        {
            await storage.DeleteAsync(storageKey, cancellationToken);
            return Result<AtmacaCardDocumentDetails>.Failure(documentResult.Error!);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception saveException)
        {
            try
            {
                await storage.DeleteAsync(storageKey, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(
                    cleanupException,
                    "Could not remove stored document {StorageKey} after its database record failed to save.",
                    storageKey);
                throw new AggregateException(
                    "Saving the document record failed and its stored file could not be removed.",
                    saveException,
                    cleanupException);
            }

            throw;
        }

        return Result<AtmacaCardDocumentDetails>.Success(ToDetails(documentResult.Value!));
    }

    private static async Task<Result<ValidatedFile>> ReadAndValidateFileAsync(
        RecordAtmacaCardDocumentCommand command,
        CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(command.FileName?.Trim() ?? string.Empty);
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string? contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };
        if (contentType is null ||
            !string.Equals(command.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ValidatedFile>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_FILE_TYPE_INVALID",
                "Only PDF, JPEG, and PNG files are accepted."));
        }

        await using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        int read;
        while ((read = await command.Content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaximumFileSizeBytes)
                return Result<ValidatedFile>.Failure(Error.Create(
                    "ATMACA_CARD_DOCUMENT_FILE_SIZE_INVALID",
                    "File size must not exceed 10 MB."));
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        byte[] bytes = buffer.ToArray();
        if (bytes.LongLength != command.FileSizeBytes || !HasExpectedSignature(bytes, extension))
            return Result<ValidatedFile>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_FILE_CONTENT_INVALID",
                "The file content does not match its file type or reported size."));

        return Result<ValidatedFile>.Success(new ValidatedFile(bytes, extension, contentType));
    }

    private static bool HasExpectedSignature(byte[] bytes, string extension) => extension switch
    {
        ".pdf" => bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
        ".png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        ".jpg" or ".jpeg" => bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        _ => false
    };

    private static Result<AtmacaCardDocumentDetails> InvalidFileSize() =>
        Result<AtmacaCardDocumentDetails>.Failure(Error.Create(
            "ATMACA_CARD_DOCUMENT_FILE_SIZE_INVALID",
            "File size must be between 1 byte and 10 MB."));

    internal static AtmacaCardDocumentDetails ToDetails(AtmacaCardDocument document) =>
        new(
            document.Id,
            document.DocumentType,
            document.Title,
            document.Issuer,
            document.IssuedOn,
            document.OriginalFileName,
            document.ContentType,
            document.FileSizeBytes);

    private sealed record ValidatedFile(byte[] Content, string Extension, string ContentType);
}
