namespace ProjectAtmaca.Domain.SeasonTeams;

public interface ISeasonTeamRepository
{
    Task AddAsync(
        SeasonTeam seasonTeam,
        CancellationToken cancellationToken = default);

    Task<SeasonTeam?> GetByIdAsync(
        SeasonTeamId id,
        CancellationToken cancellationToken = default);
}
