using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Decisions.ApplyParticipationClassification;
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
        DecisionApplicationOperationId operationId,
        DecisionId decisionId,
        DecisionRevision expectedRevision,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        // X.20 — Claim the logical operation identity first.
        //
        // UPDLOCK + HOLDLOCK protects both an existing key and
        // the key range for an operation that does not exist yet.
        Guid? existingOperationId =
            await _dbContext.Database
                .SqlQuery<Guid?>($"""
                SELECT [OperationId] AS [Value]
                FROM [DecisionApplicationOperations]
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE [OperationId] = {operationId.Value}
                """)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (existingOperationId is not null)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _dbContext.ChangeTracker.Clear();

            return DecisionAuthorityCommitOutcome
                .OperationAlreadyExists;
        }

        // X.19 — Claim the exact Decision authority.
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

            return DecisionAuthorityCommitOutcome
                .AuthorityLost;
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return DecisionAuthorityCommitOutcome.Committed;
    }
}
