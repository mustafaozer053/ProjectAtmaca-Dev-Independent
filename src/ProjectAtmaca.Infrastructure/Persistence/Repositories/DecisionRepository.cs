using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Repositories;

public sealed class DecisionRepository
    : IDecisionRepository
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public DecisionRepository(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Decision?> GetByIdAsync(
        DecisionId id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<Decision>()
            .FindAsync(
                [id.Value],
                cancellationToken);
    }
}
