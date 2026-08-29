using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionApplicationPersistenceRoundTripTests
{
    [Fact]
    public async Task DecisionApplication_Should_RoundTrip_ThroughSqlServer()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        ParticipationId participationId =
            ParticipationId.New();

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                participationId);

        DecisionRevision appliedRevision =
            DecisionRevision.From(3);

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                28,
                9,
                15,
                0,
                TimeSpan.Zero);

        DecisionApplication decisionApplication =
            DecisionApplication.Create(
                decisionId,
                target,
                appliedRevision,
                appliedAtUtc);

        DecisionApplicationId applicationId =
            decisionApplication.DecisionApplicationId;

        // Act — persist
        await using (
            ProjectAtmacaDbContext setupContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            setupContext
                .Set<DecisionApplication>()
                .Add(decisionApplication);

            await setupContext
                .SaveChangesAsync();
        }

        // Act — reload through a new DbContext
        DecisionApplication? reloaded;

        await using (
            ProjectAtmacaDbContext reloadContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            reloaded =
                await reloadContext
                    .Set<DecisionApplication>()
                    .FindAsync(
                        [applicationId.Value]);
        }

        // Assert
        reloaded
            .Should()
            .NotBeNull();

        reloaded!.DecisionApplicationId
            .Should()
            .Be(applicationId);

        reloaded.DecisionId
            .Should()
            .Be(decisionId);

        reloaded.Target
            .Should()
            .Be(target);

        reloaded.AppliedDecisionRevision
            .Should()
            .Be(appliedRevision);

        reloaded.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }
}
