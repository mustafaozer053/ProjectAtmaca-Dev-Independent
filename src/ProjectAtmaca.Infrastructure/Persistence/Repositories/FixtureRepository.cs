using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class FixtureRepository(ProjectAtmacaDbContext dbContext) : IFixtureRepository
{
    public Task<Fixture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Fixture>()
            .Include(x => x.SquadMembers)
            .Include(x => x.MatchEvents)
            .Include(x => x.ScoreEvents)
            .Include(x => x.Corrections)
            .SingleOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id, cancellationToken);

    public async Task AddAsync(Fixture fixture, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Fixture>().AddAsync(fixture, cancellationToken);

    public async Task<IReadOnlyList<Fixture>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<Fixture>()
            .AsNoTracking()
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);
}
