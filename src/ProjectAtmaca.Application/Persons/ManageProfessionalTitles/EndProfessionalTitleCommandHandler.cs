using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Application.Persons.ManageProfessionalTitles;

public sealed class EndProfessionalTitleCommandHandler(
    IActorAuthorizationService authorizationService,
    IPersonRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        EndProfessionalTitleCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageProfessionalTitles,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (command.PersonId == Guid.Empty || command.TitleId == Guid.Empty)
            return Result.Failure(Error.Create("PERSON_ID_INVALID", "Person and title ids are required."));

        Person? person = await repository.GetByIdAsync(command.PersonId, cancellationToken);
        if (person is null)
            return Result.Failure(Error.Create("PERSON_NOT_FOUND", "Person was not found."));

        var result = person.EndProfessionalTitle(command.TitleId, command.EndedOn);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record EndProfessionalTitleCommand(
    Guid PersonId,
    Guid TitleId,
    DateOnly EndedOn);
