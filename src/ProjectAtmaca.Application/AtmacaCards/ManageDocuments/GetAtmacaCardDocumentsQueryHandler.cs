using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.AtmacaCards.ManageDocuments;

public sealed class GetAtmacaCardDocumentsQueryHandler(
    IActorAuthorizationService authorizationService,
    IAtmacaCardRepository repository)
{
    public async Task<Result<IReadOnlyList<AtmacaCardDocumentDetails>>> Handle(
        AtmacaCardId atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ReadAtmacaCardDocuments,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<AtmacaCardDocumentDetails>>.Failure(authorization.Error!);

        if (atmacaCardId.Value == Guid.Empty)
            return Result<IReadOnlyList<AtmacaCardDocumentDetails>>.Failure(AtmacaCardApplicationErrors.InvalidId);

        var card = await repository.GetByIdAsync(atmacaCardId, cancellationToken);
        if (card is null)
            return Result<IReadOnlyList<AtmacaCardDocumentDetails>>.Failure(AtmacaCardApplicationErrors.NotFound);

        IReadOnlyList<AtmacaCardDocumentDetails> documents = card.Documents
            .OrderByDescending(document => document.IssuedOn)
            .ThenBy(document => document.Title)
            .Select(RecordAtmacaCardDocumentCommandHandler.ToDetails)
            .ToList();
        return Result<IReadOnlyList<AtmacaCardDocumentDetails>>.Success(documents);
    }
}
