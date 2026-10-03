using Microsoft.Extensions.Logging;
using ProjectAtmaca.Application.Abstractions.Files;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManagePhoto;

public sealed class SaveAtmacaCardPhotoCommandHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IAtmacaCardDocumentStorage storage,
    IUnitOfWork unitOfWork,
    ILogger<SaveAtmacaCardPhotoCommandHandler> logger)
{
    private const int MaximumFileSizeBytes = 5 * 1024 * 1024;

    public async Task<Result> Handle(
        SaveAtmacaCardPhotoCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageAtmacaCardDocuments,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (command.AtmacaCardId.Value == Guid.Empty)
            return Result.Failure(AtmacaCardApplicationErrors.InvalidId);

        if (command.FileSizeBytes is < 1 or > MaximumFileSizeBytes)
            return InvalidFile("Photo must be between 1 byte and 5 MB.");

        var card = await repository.GetByIdAsync(command.AtmacaCardId, cancellationToken);
        if (card is null)
            return Result.Failure(AtmacaCardApplicationErrors.NotFound);

        string extension = Path.GetExtension(Path.GetFileName(command.FileName)).ToLowerInvariant();
        string? contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };
        if (contentType is null ||
            !string.Equals(command.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
            return InvalidFile("Only JPEG and PNG photos are accepted.");

        await using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        int read;
        while ((read = await command.Content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaximumFileSizeBytes)
                return InvalidFile("Photo must not exceed 5 MB.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        byte[] bytes = buffer.ToArray();
        if (bytes.LongLength != command.FileSizeBytes || !HasExpectedSignature(bytes, extension))
            return InvalidFile("Photo content does not match its file type or reported size.");

        string storageKey =
            $"atmaca-cards/{command.AtmacaCardId.Value:N}/photo-{Guid.NewGuid():N}{extension}";
        await using var photoContent = new MemoryStream(bytes, writable: false);
        await storage.StoreAsync(storageKey, photoContent, cancellationToken);

        string? previousStorageKey = card.PhotoStorageKey;
        var photoResult = card.SetPhoto(storageKey, contentType);
        if (photoResult.IsFailure)
        {
            await storage.DeleteAsync(storageKey, cancellationToken);
            return photoResult;
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
                    "Could not remove stored photo {StorageKey} after its database record failed to save.",
                    storageKey);
                throw new AggregateException(
                    "Saving the photo reference failed and its stored file could not be removed.",
                    saveException,
                    cleanupException);
            }

            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousStorageKey))
        {
            try
            {
                await storage.DeleteAsync(previousStorageKey, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(
                    cleanupException,
                    "Could not remove replaced photo {StorageKey}.",
                    previousStorageKey);
            }
        }

        return Result.Success();
    }

    private static bool HasExpectedSignature(byte[] bytes, string extension) => extension switch
    {
        ".png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        ".jpg" or ".jpeg" => bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        _ => false
    };

    private static Result InvalidFile(string message) =>
        Result.Failure(Error.Create("ATMACA_CARD_PHOTO_FILE_INVALID", message));
}

public sealed record SaveAtmacaCardPhotoCommand(
    AtmacaCardId AtmacaCardId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content);
