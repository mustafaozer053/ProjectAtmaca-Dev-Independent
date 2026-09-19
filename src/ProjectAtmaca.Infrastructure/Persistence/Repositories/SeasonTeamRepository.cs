using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class SeasonTeamRepository : ISeasonTeamRepository
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public SeasonTeamRepository(ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        SeasonTeam seasonTeam,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seasonTeam);
        await _dbContext.Set<SeasonTeam>().AddAsync(
            seasonTeam,
            cancellationToken);
    }

    public Task<SeasonTeam?> GetByIdAsync(
        SeasonTeamId id,
        CancellationToken cancellationToken = default) =>
        _dbContext.Set<SeasonTeam>()
            .Include(x => x.Memberships)
            .SingleOrDefaultAsync(
                seasonTeam => EF.Property<Guid>(seasonTeam, "Id") == id.Value,
                cancellationToken);

    public async Task<IReadOnlyList<SeasonTeam>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.Set<SeasonTeam>()
            .Include(x => x.Memberships)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
}
