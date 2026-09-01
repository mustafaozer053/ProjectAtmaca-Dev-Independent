using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class
    DecisionApplicationOperationLockSqlMechanismTests
{
    [Fact]
    public async Task
    OperationClaim_Should_SerializeCompetingClaimForSameOperationId()
    {
        // Arrange
        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DecisionId decisionId =
            DecisionId.New();

        DecisionRevision revision =
            DecisionRevision.Initial;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                13,
                0,
                0,
                TimeSpan.Zero);

        await using var firstContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        await using var secondContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        await using var firstTransaction =
            await firstContext.Database
                .BeginTransactionAsync(
                    CancellationToken.None);

        // T1 claims the currently absent operation key.
        Guid? firstClaim =
            await firstContext.Database
                .SqlQuery<Guid?>($"""
                    SELECT [OperationId] AS [Value]
                    FROM [DecisionApplicationOperations]
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE [OperationId] =
                        {operationId.Value}
                    """)
                .SingleOrDefaultAsync(
                    CancellationToken.None);

        firstClaim
            .Should()
            .BeNull();

        // T2 attempts to claim the same operation key.
        Task<Guid?> secondClaimTask =
            ClaimOperationAsync(
                secondContext,
                operationId);

        // T2 must remain blocked while T1 owns
        // the key-range lock.
        Task completedTask =
            await Task.WhenAny(
                secondClaimTask,
                Task.Delay(300));

        completedTask
            .Should()
            .NotBeSameAs(secondClaimTask);

        // T1 makes the operation durable while still
        // holding the claim.
        DecisionApplicationOperation operation =
            new(
                operationId,
                decisionId,
                revision,
                appliedAtUtc);

        firstContext
            .Set<DecisionApplicationOperation>()
            .Add(operation);

        await firstContext.SaveChangesAsync(
            CancellationToken.None);

        await firstTransaction.CommitAsync(
            CancellationToken.None);

        // Only after T1 commits may T2's claim complete.
        Guid? secondClaim =
            await secondClaimTask;

        // Assert
        secondClaim
            .Should()
            .Be(operationId.Value);

        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        DecisionApplicationOperation persistedOperation =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.OperationId ==
                        operationId,
                    CancellationToken.None);

        persistedOperation.DecisionId
            .Should()
            .Be(decisionId);

        persistedOperation.DecisionRevision
            .Should()
            .Be(revision);

        persistedOperation.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }

    private static async Task<Guid?> ClaimOperationAsync(
        ProjectAtmacaDbContext context,
        DecisionApplicationOperationId operationId)
    {
        await using var transaction =
            await context.Database
                .BeginTransactionAsync(
                    CancellationToken.None);

        Guid? claim =
            await context.Database
                .SqlQuery<Guid?>($"""
                    SELECT [OperationId] AS [Value]
                    FROM [DecisionApplicationOperations]
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE [OperationId] =
                        {operationId.Value}
                    """)
                .SingleOrDefaultAsync(
                    CancellationToken.None);

        await transaction.RollbackAsync(
            CancellationToken.None);

        return claim;
    }
}
