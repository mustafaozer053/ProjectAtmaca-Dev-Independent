using ProjectAtmaca.Application.Abstractions.Files;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageDocuments;

public sealed class OpenAtmacaCardDocumentQueryHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository,
    IAtmacaCardDocumentStorage storage)
{
    public async Task<Result<OpenedAtmacaCardDocument>> Handle(
        AtmacaCardId atmacaCardId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ReadAtmacaCardDocuments,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<OpenedAtmacaCardDocument>.Failure(authorization.Error!);

        if (atmacaCardId.Value == Guid.Empty || documentId == Guid.Empty)
            return Result<OpenedAtmacaCardDocument>.Failure(AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(atmacaCardId, cancellationToken);
        if (card is null)
            return Result<OpenedAtmacaCardDocument>.Failure(AtmacaCardApplicationErrors.NotFound);

        AtmacaCardDocument? document = card.Documents.SingleOrDefault(item => item.Id == documentId);
        if (document is null)
            return Result<OpenedAtmacaCardDocument>.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_NOT_FOUND",
                "The document was not found."));

        var stream = await storage.OpenReadAsync(document.StorageKey, cancellationToken);
        return Result<OpenedAtmacaCardDocument>.Success(new OpenedAtmacaCardDocument(
            stream,
            document.ContentType,
            document.OriginalFileName));
    }
}

public sealed record OpenedAtmacaCardDocument(Stream Content, string ContentType, string FileName);
