using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.AddMembership;

public sealed class AddSeasonTeamMembershipCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddSeasonTeamMembershipCommandHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SeasonTeamMembershipId>> Handle(
        AddSeasonTeamMembershipCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.AddMembership, cancellationToken);
        if (authorization.IsFailure)
            return Result<SeasonTeamMembershipId>.Failure(authorization.Error!);

        if (command.SeasonTeamId == Guid.Empty ||
            command.AtmacaCardId == Guid.Empty)
        {
            return Result<SeasonTeamMembershipId>.Failure(
                Error.Create(
                    "SeasonTeam.InvalidId",
                    "Season team and Atmaca card ids are required."));
        }

        var team = await _repository.GetByIdAsync(
            SeasonTeamId.From(command.SeasonTeamId), cancellationToken);
        if (team is null)
            return Result<SeasonTeamMembershipId>.Failure(
                SeasonTeamApplicationErrors.NotFound);

        var period = AssignmentPeriod.Create(command.StartDate, command.EndDate);
        if (period.IsFailure)
            return Result<SeasonTeamMembershipId>.Failure(period.Error!);

        var result = team.AddMembership(
            AtmacaCardId.From(command.AtmacaCardId), period.Value!);
        if (result.IsFailure)
            return Result<SeasonTeamMembershipId>.Failure(result.Error!);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SeasonTeamMembershipId>.Success(
            result.Value!.SeasonTeamMembershipId);
    }
}
