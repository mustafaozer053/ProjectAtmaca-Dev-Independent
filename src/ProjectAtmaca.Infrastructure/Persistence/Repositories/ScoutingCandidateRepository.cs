using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Scouting;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class ScoutingCandidateRepository(ProjectAtmacaDbContext dbContext) : IScoutingCandidateRepository
{
    public Task<ScoutingCandidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ScoutingCandidate>().Include(x => x.Observations)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(ScoutingCandidate candidate, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.Set<ScoutingCandidate>().AddAsync(candidate, cancellationToken);
        entry.Property("IdentityKey").CurrentValue = candidate.IdentityNumber?.DisplayValue;
    }

    public Task<bool> ExistsByIdentityNumberAsync(IdentityNumber identityNumber, CancellationToken cancellationToken = default)
    {
        var key = identityNumber.DisplayValue;
        return dbContext.Set<ScoutingCandidate>()
            .AnyAsync(x => EF.Property<string>(x, "IdentityKey") == key, cancellationToken);
    }

    public Task<bool> ExistsByIdentityNumberAsync(IdentityNumber identityNumber, Guid excludeCandidateId, CancellationToken cancellationToken = default)
    {
        var key = identityNumber.DisplayValue;
        return dbContext.Set<ScoutingCandidate>()
            .AnyAsync(x => x.Id != excludeCandidateId && EF.Property<string>(x, "IdentityKey") == key, cancellationToken);
    }

    public Task<bool> ExistsByRegisteredPersonAsync(Guid personId, Guid excludeCandidateId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ScoutingCandidate>()
            .AnyAsync(x => x.Id != excludeCandidateId && x.RegisteredPersonId == personId, cancellationToken);

    public void RefreshIdentityKey(ScoutingCandidate candidate) =>
        dbContext.Entry(candidate).Property("IdentityKey").CurrentValue = candidate.IdentityNumber?.DisplayValue;
    public async Task<IReadOnlyList<ScoutingCandidate>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<ScoutingCandidate>().Include(x => x.Observations).AsNoTracking()
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
}
