using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.EndMembershipAssignment;

public sealed class EndSeasonTeamMembershipAssignmentCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EndSeasonTeamMembershipAssignmentCommandHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        EndSeasonTeamMembershipAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.EndMembershipAssignment,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        if (command.SeasonTeamId == Guid.Empty ||
            command.MembershipId == Guid.Empty ||
            command.AssignmentId == Guid.Empty)
        {
            return Result.Failure(
                Error.Create(
                    "SeasonTeam.InvalidId",
                    "Season team, membership and assignment ids are required."));
        }

        SeasonTeam? team = await _repository.GetByIdAsync(
            SeasonTeamId.From(command.SeasonTeamId),
            cancellationToken);
        if (team is null)
            return Result.Failure(SeasonTeamApplicationErrors.NotFound);

        Result result = team.EndMembershipAssignment(
            SeasonTeamMembershipId.From(command.MembershipId),
            SeasonTeamMembershipAssignmentId.From(command.AssignmentId),
            command.EndDate);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
