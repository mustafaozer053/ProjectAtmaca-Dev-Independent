using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.EndMembership;

public sealed class EndSeasonTeamMembershipCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EndSeasonTeamMembershipCommandHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        EndSeasonTeamMembershipCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.EndMembership,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (command.SeasonTeamId == Guid.Empty ||
            command.MembershipId == Guid.Empty)
        {
            return Result.Failure(
                Error.Create(
                    "SeasonTeam.InvalidId",
                    "Season team and membership ids are required."));
        }

        var team = await _repository.GetByIdAsync(
            SeasonTeamId.From(command.SeasonTeamId), cancellationToken);
        if (team is null)
            return Result.Failure(SeasonTeamApplicationErrors.NotFound);

        var result = team.EndMembership(
            SeasonTeamMembershipId.From(command.MembershipId),
            command.EndDate);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
