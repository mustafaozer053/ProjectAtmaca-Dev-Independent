using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionPersistenceRoundTripTests
{
    [Fact]
    public async Task Decision_Should_RoundTrip_ThroughSqlServer()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationId participationId =
            ParticipationId.New();

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                28,
                10,
                0,
                0,
                TimeSpan.FromHours(3));

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.Present,
                ParticipationCondition.Late,
                joinedAt,
                leftAt);

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                snapshot,
                ParticipationClassificationEffect.Present());

        DecisionId successorDecisionId =
            DecisionId.New();

        decision.SupersedeBy(
            successorDecisionId);

        DecisionId decisionId =
            decision.DecisionId;

        // Act — persist
        await using (
            ProjectAtmacaDbContext setupContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            setupContext
                .Set<Decision>()
                .Add(decision);

            await setupContext
                .SaveChangesAsync();
        }

        // Act — reload through a new DbContext
        Decision? reloaded;

        await using (
            ProjectAtmacaDbContext reloadContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            reloaded =
                await reloadContext
                    .Set<Decision>()
                    .FindAsync(
                        [decisionId.Value]);
        }

        // Assert
        reloaded
            .Should()
            .NotBeNull();

        reloaded!.DecisionId
            .Should()
            .Be(decisionId);

        reloaded.Revision
            .Should()
            .Be(DecisionRevision.Initial);

        reloaded.Target
            .Should()
            .Be(decision.Target);

        reloaded.Snapshot.ActivityReference
            .Should()
            .Be(activityReference);

        reloaded.Snapshot.AtmacaCardId
            .Should()
            .Be(atmacaCardId);

        reloaded.Snapshot.Status
            .Should()
            .Be(ParticipationStatus.Present);

        reloaded.Snapshot.Condition
            .Should()
            .Be(ParticipationCondition.Late);

        reloaded.Snapshot.JoinedAt
            .Should()
            .Be(joinedAt);

        reloaded.Snapshot.LeftAt
            .Should()
            .Be(leftAt);

        reloaded.Effect
            .Should()
            .Be(
                ParticipationClassificationEffect
                    .Present());

        reloaded.SupersededByDecisionId
            .Should()
            .Be(successorDecisionId);
    }
}
