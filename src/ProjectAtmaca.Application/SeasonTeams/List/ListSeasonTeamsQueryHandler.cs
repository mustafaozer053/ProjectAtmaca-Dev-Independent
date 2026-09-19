using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.List;

public sealed class ListSeasonTeamsQueryHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;

    public ListSeasonTeamsQueryHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository)
    {
        _authorizationService = authorizationService;
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<SeasonTeamListItem>>> Handle(
        ListSeasonTeamsQuery query,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.SeasonTeams.List, cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<SeasonTeamListItem>>.Failure(authorization.Error!);

        var values = await _repository.ListAsync(cancellationToken);
        return Result<IReadOnlyList<SeasonTeamListItem>>.Success(
            values.Select(x => new SeasonTeamListItem(
                x.SeasonTeamId.Value,
                x.SeasonId.Value,
                x.OrganizationId.Value,
                x.AgeGroupId,
                x.Name,
                x.Memberships.Count,
                x.Status == SeasonTeamStatus.Active)).ToList());
    }
}
