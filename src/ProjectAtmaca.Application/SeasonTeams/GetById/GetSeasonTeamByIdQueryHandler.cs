using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.GetById;

public sealed class GetSeasonTeamByIdQueryHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;

    public GetSeasonTeamByIdQueryHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository)
    {
        _authorizationService = authorizationService;
        _repository = repository;
    }

    public async Task<Result<SeasonTeamDetails>> Handle(
        GetSeasonTeamByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.List,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<SeasonTeamDetails>.Failure(authorization.Error!);

        var seasonTeam = await _repository.GetByIdAsync(
            query.SeasonTeamId,
            cancellationToken);
        if (seasonTeam is null)
            return Result<SeasonTeamDetails>.Failure(
                SeasonTeamApplicationErrors.NotFound);

        var today = DateTime.UtcNow.Date;
        return Result<SeasonTeamDetails>.Success(
            new SeasonTeamDetails(
                seasonTeam.SeasonTeamId.Value,
                seasonTeam.SeasonId.Value,
                seasonTeam.OrganizationId.Value,
                seasonTeam.AgeGroupId,
                seasonTeam.Name,
                seasonTeam.Status == SeasonTeamStatus.Active,
                seasonTeam.Memberships
                    .OrderBy(x => x.Period.StartDate)
                    .ThenBy(x => x.AtmacaCardId.Value)
                    .Select(x => new SeasonTeamMembershipDetails(
                        x.SeasonTeamMembershipId.Value,
                        x.AtmacaCardId.Value,
                        x.Period.StartDate,
                        x.Period.EndDate,
                        x.IsActiveOn(today),
                        x.Assignments
                            .OrderBy(y => y.Period.StartDate)
                            .ThenBy(y => y.Kind)
                            .ThenBy(y => y.DefinitionId)
                            .Select(y => new SeasonTeamMembershipAssignmentDetails(
                                y.SeasonTeamMembershipAssignmentId.Value,
                                y.Kind,
                                y.DefinitionId,
                                y.DisplayNameSnapshot,
                                y.Period.StartDate,
                                y.Period.EndDate,
                                y.IsActiveOn(today)))
                            .ToList()))
                    .ToList()));
    }
}
