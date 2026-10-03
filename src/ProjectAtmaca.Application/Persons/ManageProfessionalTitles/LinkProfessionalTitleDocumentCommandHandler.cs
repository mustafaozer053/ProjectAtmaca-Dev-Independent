using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Application.Persons.ManageProfessionalTitles;

public sealed class LinkProfessionalTitleDocumentCommandHandler(
    IActorAuthorizationService authorizationService,
    IPersonRepository personRepository,
    IAtmacaCardReader atmacaCardReader,
    IAtmacaCardRepository atmacaCardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        LinkProfessionalTitleDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageProfessionalTitles,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (command.PersonId == Guid.Empty ||
            command.ProfessionalTitleId == Guid.Empty ||
            command.DocumentId == Guid.Empty)
        {
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_DOCUMENT_REQUIRED",
                "Person, title, and document ids are required."));
        }

        Person? person = await personRepository.GetByIdAsync(command.PersonId, cancellationToken);
        if (person is null)
            return Result.Failure(Error.Create("PERSON_NOT_FOUND", "Person was not found."));

        PersonProfessionalTitle? title = person.ProfessionalTitles.SingleOrDefault(
            item => item.Id == command.ProfessionalTitleId);
        if (title is null)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_NOT_FOUND",
                "Professional title was not found."));

        var cardSummary = await atmacaCardReader.GetByPersonIdAsync(
            command.PersonId,
            cancellationToken);
        if (cardSummary is null)
            return Result.Failure(Error.Create(
                "ATMACA_CARD_NOT_FOUND",
                "The person does not have an Atmaca Card."));

        var card = await atmacaCardRepository.GetByIdAsync(
            AtmacaCardId.From(cardSummary.AtmacaCardId),
            cancellationToken);
        if (card is null || card.Documents.All(document => document.Id != command.DocumentId))
            return Result.Failure(Error.Create(
                "ATMACA_CARD_DOCUMENT_NOT_FOUND",
                "The document is not registered on this person's Atmaca Card."));

        var result = title.LinkEvidenceDocument(command.DocumentId);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record LinkProfessionalTitleDocumentCommand(
    Guid PersonId,
    Guid ProfessionalTitleId,
    Guid DocumentId);
