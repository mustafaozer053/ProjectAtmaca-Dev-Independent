using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

namespace ProjectAtmaca.Infrastructure.Persistence.Decisions;

public sealed class DecisionApplicationOperationStore
    : IDecisionApplicationOperationStore
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public DecisionApplicationOperationStore(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public async Task<DecisionApplicationOperation?> GetByIdAsync(
        DecisionApplicationOperationId operationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<DecisionApplicationOperation>()
            .FindAsync(
                [operationId],
                cancellationToken);
    }

    public async Task AddAsync(
        DecisionApplicationOperation operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        await _dbContext
            .Set<DecisionApplicationOperation>()
            .AddAsync(
                operation,
                cancellationToken);
    }
}
