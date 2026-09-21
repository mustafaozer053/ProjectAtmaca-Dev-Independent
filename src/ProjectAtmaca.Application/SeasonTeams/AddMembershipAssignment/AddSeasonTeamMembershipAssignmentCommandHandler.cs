using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.AddMembershipAssignment;

public sealed class AddSeasonTeamMembershipAssignmentCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddSeasonTeamMembershipAssignmentCommandHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SeasonTeamMembershipAssignmentId>> Handle(
        AddSeasonTeamMembershipAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.AddMembershipAssignment,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<SeasonTeamMembershipAssignmentId>.Failure(
                authorization.Error!);

        if (command.SeasonTeamId == Guid.Empty ||
            command.MembershipId == Guid.Empty)
        {
            return Result<SeasonTeamMembershipAssignmentId>.Failure(
                Error.Create(
                    "SeasonTeam.InvalidId",
                    "Season team and membership ids are required."));
        }

        SeasonTeam? team = await _repository.GetByIdAsync(
            SeasonTeamId.From(command.SeasonTeamId),
            cancellationToken);
        if (team is null)
        {
            return Result<SeasonTeamMembershipAssignmentId>.Failure(
                SeasonTeamApplicationErrors.NotFound);
        }

        Result<AssignmentPeriod> period = AssignmentPeriod.Create(
            command.StartDate,
            command.EndDate);
        if (period.IsFailure)
            return Result<SeasonTeamMembershipAssignmentId>.Failure(period.Error!);

        Result<SeasonTeamMembershipAssignment> result = team.AddMembershipAssignment(
            SeasonTeamMembershipId.From(command.MembershipId),
            command.Kind,
            command.DefinitionId,
            command.DisplayNameSnapshot,
            period.Value!);
        if (result.IsFailure)
            return Result<SeasonTeamMembershipAssignmentId>.Failure(result.Error!);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SeasonTeamMembershipAssignmentId>.Success(
            result.Value!.SeasonTeamMembershipAssignmentId);
    }
}
