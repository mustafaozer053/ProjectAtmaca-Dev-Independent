using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Application.Persons.ManageProfessionalTitles;

public sealed class UpdateProfessionalTitleCommandHandler(
    IActorAuthorizationService authorizationService,
    IPersonRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        UpdateProfessionalTitleCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageProfessionalTitles,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (command.PersonId == Guid.Empty)
            return Result.Failure(Error.Create("PERSON_ID_INVALID", "Person id is required."));

        Person? person = await repository.GetByIdAsync(command.PersonId, cancellationToken);
        if (person is null)
            return Result.Failure(Error.Create("PERSON_NOT_FOUND", "Person was not found."));

        var result = person.UpdateProfessionalTitle(
            command.TitleId,
            command.Title,
            command.StartedOn);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record UpdateProfessionalTitleCommand(
    Guid PersonId,
    Guid TitleId,
    string Title,
    DateOnly StartedOn);
