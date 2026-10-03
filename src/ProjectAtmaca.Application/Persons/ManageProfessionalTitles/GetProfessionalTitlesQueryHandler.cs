using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Application.Persons.ManageProfessionalTitles;

public sealed class GetProfessionalTitlesQueryHandler(
    IActorAuthorizationService authorizationService,
    IPersonRepository repository)
{
    public async Task<Result<IReadOnlyList<ProfessionalTitleDetails>>> Handle(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ReadProfessionalTitles,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<ProfessionalTitleDetails>>.Failure(authorization.Error!);

        if (personId == Guid.Empty)
            return Result<IReadOnlyList<ProfessionalTitleDetails>>.Failure(Error.Create(
                "PERSON_ID_INVALID",
                "Person id is required."));

        Person? person = await repository.GetByIdAsync(personId, cancellationToken);
        if (person is null)
            return Result<IReadOnlyList<ProfessionalTitleDetails>>.Failure(Error.Create(
                "PERSON_NOT_FOUND",
                "Person was not found."));

        IReadOnlyList<ProfessionalTitleDetails> titles = person.ProfessionalTitles
            .OrderByDescending(title => title.EndedOn is null)
            .ThenByDescending(title => title.StartedOn)
            .ThenBy(title => title.Title)
            .Select(ToDetails)
            .ToList();
        return Result<IReadOnlyList<ProfessionalTitleDetails>>.Success(titles);
    }

    internal static ProfessionalTitleDetails ToDetails(PersonProfessionalTitle title) =>
        new(
            title.Id,
            title.Title,
            title.StartedOn,
            title.EndedOn,
            title.EvidenceDocuments.Select(link => link.AtmacaCardDocumentId).ToList());
}
