using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Application.Persons.ManageProfessionalTitles;

public sealed class AddProfessionalTitleCommandHandler(
    IActorAuthorizationService authorizationService,
    IPersonRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<ProfessionalTitleDetails>> Handle(
        AddProfessionalTitleCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Persons.ManageProfessionalTitles,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<ProfessionalTitleDetails>.Failure(authorization.Error!);

        if (command.PersonId == Guid.Empty)
            return Result<ProfessionalTitleDetails>.Failure(Error.Create(
                "PERSON_ID_INVALID",
                "Person id is required."));

        Person? person = await repository.GetByIdAsync(command.PersonId, cancellationToken);
        if (person is null)
            return Result<ProfessionalTitleDetails>.Failure(Error.Create(
                "PERSON_NOT_FOUND",
                "Person was not found."));

        var result = person.AddProfessionalTitle(command.Title, command.StartedOn);
        if (result.IsFailure)
            return Result<ProfessionalTitleDetails>.Failure(result.Error!);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProfessionalTitleDetails>.Success(
            GetProfessionalTitlesQueryHandler.ToDetails(result.Value!));
    }
}

public sealed record AddProfessionalTitleCommand(
    Guid PersonId,
    string Title,
    DateOnly StartedOn);
