using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.GetById;

public sealed class GetSeasonTeamByIdQueryHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ISeasonTeamRepository _repository;
    private readonly IAtmacaCardReader _atmacaCardReader;
    private readonly ISeasonPeriodReader _seasonPeriodReader;

    public GetSeasonTeamByIdQueryHandler(
        IActorAuthorizationService authorizationService,
        ISeasonTeamRepository repository,
        IAtmacaCardReader atmacaCardReader,
        ISeasonPeriodReader seasonPeriodReader)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _atmacaCardReader = atmacaCardReader;
        _seasonPeriodReader = seasonPeriodReader;
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

        var seasonPeriod = await _seasonPeriodReader.GetPeriodAsync(
            seasonTeam.SeasonId.Value,
            cancellationToken);
        if (seasonPeriod is null)
            return Result<SeasonTeamDetails>.Failure(Error.Create(
                "SEASON_NOT_FOUND",
                "Season for the team was not found."));

        var today = DateTime.UtcNow.Date;
        var cardIds = seasonTeam.Memberships
            .Select(x => x.AtmacaCardId.Value)
            .Distinct()
            .ToList();
        var cardSummaries = await _atmacaCardReader.GetSummariesAsync(
            cardIds,
            cancellationToken);

        return Result<SeasonTeamDetails>.Success(
            new SeasonTeamDetails(
                seasonTeam.SeasonTeamId.Value,
                seasonTeam.SeasonId.Value,
                seasonTeam.OrganizationId.Value,
                seasonTeam.AgeGroupId,
                seasonTeam.Name,
                seasonTeam.Status == SeasonTeamStatus.Active,
                seasonPeriod.StartDate,
                seasonPeriod.EndDate,
                seasonTeam.Memberships
                    .OrderBy(x => x.Period.StartDate)
                    .ThenBy(x => x.AtmacaCardId.Value)
                    .Select(x =>
                    {
                        cardSummaries.TryGetValue(
                            x.AtmacaCardId.Value,
                            out var cardSummary);
                        return new SeasonTeamMembershipDetails(
                            x.SeasonTeamMembershipId.Value,
                            x.AtmacaCardId.Value,
                            cardSummary?.FullName,
                            cardSummary?.CardNumber,
                            x.Period.StartDate,
                            x.Period.EndDate,
                            seasonPeriod.Contains(today) && x.IsActiveOn(today),
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
                                    Earlier(
                                        Earlier(y.Period.EndDate, x.Period.EndDate),
                                        seasonPeriod.EndDate),
                                    seasonPeriod.Contains(today) &&
                                        x.IsActiveOn(today) &&
                                        y.IsActiveOn(today)))
                                .ToList());
                    })
                    .ToList()));
    }

    private static DateTime? Earlier(DateTime? first, DateTime? second) =>
        first.HasValue && second.HasValue
            ? first.Value.Date <= second.Value.Date ? first.Value.Date : second.Value.Date
            : first?.Date ?? second?.Date;
}
