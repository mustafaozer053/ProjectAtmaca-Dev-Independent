using FluentAssertions;

using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionApplicationOperationStoreTests
{
    [Fact]
    public async Task GetByIdAsync_Should_ReturnPersistedOperation_ByTypedOperationId()
    {
        // Arrange
        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DecisionId decisionId =
            DecisionId.New();

        DecisionRevision decisionRevision =
            DecisionRevision.From(3);

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                11,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplicationOperation operation =
            new(
                operationId,
                decisionId,
                decisionRevision,
                appliedAtUtc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext
                .Set<DecisionApplicationOperation>()
                .Add(operation);

            await seedContext.SaveChangesAsync(
                CancellationToken.None);
        }

        await using ProjectAtmacaDbContext lookupContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        DecisionApplicationOperationStore store =
            new(lookupContext);

        // Act
        DecisionApplicationOperation? persistedOperation =
            await store.GetByIdAsync(
                operationId,
                CancellationToken.None);

        // Assert
        persistedOperation
            .Should()
            .NotBeNull();

        persistedOperation!.OperationId
            .Should()
            .Be(operationId);

        persistedOperation.DecisionId
            .Should()
            .Be(decisionId);

        persistedOperation.DecisionRevision
            .Should()
            .Be(decisionRevision);

        persistedOperation.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }
}
