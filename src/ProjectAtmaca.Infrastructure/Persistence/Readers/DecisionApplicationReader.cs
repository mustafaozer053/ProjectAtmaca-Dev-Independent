using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Decisions;
using ProjectAtmaca.Application.Decisions.ListApplicationHistory;
using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Infrastructure.Persistence.Readers;

public sealed class DecisionApplicationReader
    : IDecisionApplicationReader
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public DecisionApplicationReader(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<DecisionApplicationHistoryItem>>
        ListHistoryByDecisionAsync(
            DecisionId decisionId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<DecisionApplication>()
            .AsNoTracking()
            .Where(
                application =>
                    application.DecisionId ==
                    decisionId)
            .OrderByDescending(
                application =>
                    application.AppliedAtUtc)
            .ThenByDescending(
                application =>
                    application.Id)
            .Select(
                application =>
                    new DecisionApplicationHistoryItem(
                        application.DecisionApplicationId,
                        application.DecisionId,
                        application.Target,
                        application.AppliedDecisionRevision,
                        application.AppliedAtUtc))
            .ToListAsync(
                cancellationToken);
    }
}
