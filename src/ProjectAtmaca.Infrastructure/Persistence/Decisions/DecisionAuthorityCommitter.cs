using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Infrastructure.Persistence.Decisions;

public sealed class DecisionAuthorityCommitter
    : IDecisionAuthorityCommitter
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public DecisionAuthorityCommitter(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public async Task<DecisionAuthorityCommitOutcome> CommitAsync(
        DecisionId decisionId,
        DecisionRevision expectedRevision,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        int authorityClaim =
            await _dbContext.Database
                .SqlQuery<int>($"""
                    SELECT 1 AS [Value]
                    FROM [Decisions]
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Id] = {decisionId.Value}
                      AND [Revision] = {expectedRevision.Value}
                      AND [SupersededByDecisionId] IS NULL
                    """)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (authorityClaim == 0)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _dbContext.ChangeTracker.Clear();

            return DecisionAuthorityCommitOutcome.AuthorityLost;
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return DecisionAuthorityCommitOutcome.Committed;
    }
}
