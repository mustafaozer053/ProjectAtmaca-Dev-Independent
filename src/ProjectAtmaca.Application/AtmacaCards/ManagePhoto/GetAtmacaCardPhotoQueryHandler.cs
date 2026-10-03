using ProjectAtmaca.Application.Abstractions.Files;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManagePhoto;

public sealed class GetAtmacaCardPhotoQueryHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IAtmacaCardDocumentStorage storage)
{
    public async Task<Result<OpenedAtmacaCardPhoto>> Handle(
        AtmacaCardId atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ReadAtmacaCardDocuments,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<OpenedAtmacaCardPhoto>.Failure(authorization.Error!);

        if (atmacaCardId.Value == Guid.Empty)
            return Result<OpenedAtmacaCardPhoto>.Failure(AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(atmacaCardId, cancellationToken);
        if (card is null)
            return Result<OpenedAtmacaCardPhoto>.Failure(AtmacaCardApplicationErrors.NotFound);

        if (string.IsNullOrWhiteSpace(card.PhotoStorageKey) ||
            string.IsNullOrWhiteSpace(card.PhotoContentType))
        {
            return Result<OpenedAtmacaCardPhoto>.Failure(Error.Create(
                "ATMACA_CARD_PHOTO_NOT_FOUND",
                "No photo is registered for this card."));
        }

        var stream = await storage.OpenReadAsync(card.PhotoStorageKey, cancellationToken);
        return Result<OpenedAtmacaCardPhoto>.Success(
            new OpenedAtmacaCardPhoto(stream, card.PhotoContentType));
    }
}

public sealed record OpenedAtmacaCardPhoto(Stream Content, string ContentType);
