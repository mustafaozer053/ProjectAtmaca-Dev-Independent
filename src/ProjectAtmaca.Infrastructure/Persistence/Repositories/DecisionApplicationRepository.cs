using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Repositories;

public sealed class DecisionApplicationRepository
    : IDecisionApplicationRepository
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public DecisionApplicationRepository(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        DecisionApplication decisionApplication,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            decisionApplication);

        await _dbContext
            .Set<DecisionApplication>()
            .AddAsync(
                decisionApplication,
                cancellationToken);
    }
}
