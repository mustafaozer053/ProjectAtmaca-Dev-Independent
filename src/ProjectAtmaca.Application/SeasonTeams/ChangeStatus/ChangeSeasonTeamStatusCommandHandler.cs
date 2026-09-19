using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.ChangeStatus;

public sealed class ChangeSeasonTeamStatusCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeSeasonTeamStatusCommandHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        ChangeSeasonTeamStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.ChangeStatus,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        SeasonTeam? seasonTeam = await _repository.GetByIdAsync(
            command.SeasonTeamId,
            cancellationToken);
        if (seasonTeam is null)
            return Result.Failure(SeasonTeamApplicationErrors.NotFound);

        if (command.IsActive)
            seasonTeam.Activate();
        else
            seasonTeam.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
