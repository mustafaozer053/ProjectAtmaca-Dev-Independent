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
public sealed class DecisionApplicationOperationPersistenceRoundTripTests
{
    [Fact]
    public async Task DecisionApplicationOperation_Should_RoundTrip_ThroughSqlServer()
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
                8,
                31,
                11,
                30,
                0,
                TimeSpan.Zero);

        var operation =
            new DecisionApplicationOperation(
                operationId,
                decisionId,
                decisionRevision,
                appliedAtUtc);

        // Act — persist
        await using (
            ProjectAtmacaDbContext setupContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            setupContext
                .Set<DecisionApplicationOperation>()
                .Add(operation);

            await setupContext
                .SaveChangesAsync();
        }

        // Act — reload through a new DbContext
        DecisionApplicationOperation? reloaded;

        await using (
            ProjectAtmacaDbContext reloadContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            reloaded =
                await reloadContext
                    .Set<DecisionApplicationOperation>()
                    .FindAsync(
                        [operationId]);
        }

        // Assert
        reloaded
            .Should()
            .NotBeNull();

        reloaded!.OperationId
            .Should()
            .Be(operationId);

        reloaded.DecisionId
            .Should()
            .Be(decisionId);

        reloaded.DecisionRevision
            .Should()
            .Be(decisionRevision);

        reloaded.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }
}
