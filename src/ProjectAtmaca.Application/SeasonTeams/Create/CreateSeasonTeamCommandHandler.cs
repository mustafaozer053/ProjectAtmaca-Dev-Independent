using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.Create;

public sealed class CreateSeasonTeamCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSeasonTeamCommandHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SeasonTeamId>> Handle(
        CreateSeasonTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.Create, cancellationToken);
        if (authorization.IsFailure)
            return Result<SeasonTeamId>.Failure(authorization.Error!);

        if (command.SeasonId == Guid.Empty)
            return Result<SeasonTeamId>.Failure(SeasonTeamErrors.SeasonRequired);
        if (command.OrganizationId == Guid.Empty)
            return Result<SeasonTeamId>.Failure(SeasonTeamErrors.OrganizationRequired);
        if (command.AgeGroupId == Guid.Empty)
            return Result<SeasonTeamId>.Failure(SeasonTeamErrors.AgeGroupRequired);

        var result = SeasonTeam.Create(
            SeasonId.From(command.SeasonId),
            OrganizationId.From(command.OrganizationId),
            command.AgeGroupId,
            command.Name!);
        if (result.IsFailure)
            return Result<SeasonTeamId>.Failure(result.Error!);

        await _repository.AddAsync(result.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SeasonTeamId>.Success(result.Value!.SeasonTeamId);
    }
}
