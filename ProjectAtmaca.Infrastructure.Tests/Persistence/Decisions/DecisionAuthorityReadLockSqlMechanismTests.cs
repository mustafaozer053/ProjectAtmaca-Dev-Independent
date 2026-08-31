using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionAuthorityReadLockSqlMechanismTests
{
    [Fact]
    public async Task ReadLockAuthorityClaim_Should_Reject_WhenRevisionChanged()
    {
        // Arrange
        Participation participation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New())
            .Value!;

        Decision decision =
            Decision.CreateParticipationClassification(
                participation.ParticipationId,
                ParticipationClassificationSnapshot.Create(
                    participation.ActivityReference,
                    participation.AtmacaCardId,
                    participation.Status,
                    participation.Condition,
                    participation.JoinedAt,
                    participation.LeftAt),
                ParticipationClassificationEffect.Present());

        await using (
            var seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            seedContext.Set<Decision>().Add(
                decision);

            await seedContext.SaveChangesAsync(
                CancellationToken.None);
        }

        DecisionRevision staleRevision;

        await using (
            var staleReaderContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision staleDecision =
                await staleReaderContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id ==
                            decision.Id,
                        CancellationToken.None);

            staleRevision =
                staleDecision.Revision;
        }

        // Competing authority mutation commits first.
        await using (
            var authorityMutationContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision authoritativeDecision =
                await authorityMutationContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id ==
                            decision.Id,
                        CancellationToken.None);

            ParticipationClassificationSnapshot
                changedSnapshot =
                    ParticipationClassificationSnapshot
                        .Create(
                            participation.ActivityReference,
                            participation.AtmacaCardId,
                            ParticipationStatus.Absent,
                            participation.Condition,
                            participation.JoinedAt,
                            participation.LeftAt);

            authoritativeDecision.ReEvaluate(
                changedSnapshot,
                ParticipationClassificationEffect.Absent());

            await authorityMutationContext
                .SaveChangesAsync(
                    CancellationToken.None);
        }

        // Act
        int authorityClaim;

        await using (
            var claimContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            await using var transaction =
                await claimContext.Database
                    .BeginTransactionAsync(
                        CancellationToken.None);

            authorityClaim =
                await claimContext.Database
                    .SqlQuery<int>($"""
                        SELECT 1 AS [Value]
                        FROM [Decisions]
                            WITH (UPDLOCK, HOLDLOCK)
                        WHERE [Id] = {decision.Id}
                          AND [Revision] =
                              {staleRevision.Value}
                          AND [SupersededByDecisionId]
                              IS NULL
                        """)
                    .SingleOrDefaultAsync(
                        CancellationToken.None);

            await transaction.RollbackAsync(
                CancellationToken.None);
        }

        // Assert
        authorityClaim
            .Should()
            .Be(0);

        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision persistedDecision =
            await verificationContext
                .Set<Decision>()
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        decision.Id,
                    CancellationToken.None);

        persistedDecision.Revision
            .Should()
            .Be(
                staleRevision.Next());
    }

    [Fact]
    public async Task ReadLockAuthorityClaim_Should_Reject_WhenDecisionWasSuperseded()
    {
        // Arrange
        Participation participation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New())
            .Value!;

        Decision decision =
            Decision.CreateParticipationClassification(
                participation.ParticipationId,
                ParticipationClassificationSnapshot.Create(
                    participation.ActivityReference,
                    participation.AtmacaCardId,
                    participation.Status,
                    participation.Condition,
                    participation.JoinedAt,
                    participation.LeftAt),
                ParticipationClassificationEffect.Present());

        await using (
            var seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            seedContext.Set<Decision>().Add(
                decision);

            await seedContext.SaveChangesAsync(
                CancellationToken.None);
        }

        DecisionRevision observedRevision;

        await using (
            var staleReaderContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision observedDecision =
                await staleReaderContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id ==
                            decision.Id,
                        CancellationToken.None);

            observedRevision =
                observedDecision.Revision;
        }

        DecisionId successorDecisionId =
            DecisionId.New();

        // Competing supersession commits first.
        await using (
            var authorityMutationContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision authoritativeDecision =
                await authorityMutationContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id ==
                            decision.Id,
                        CancellationToken.None);

            authoritativeDecision.SupersedeBy(
                successorDecisionId);

            await authorityMutationContext
                .SaveChangesAsync(
                    CancellationToken.None);
        }

        // Act
        int authorityClaim;

        await using (
            var claimContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            await using var transaction =
                await claimContext.Database
                    .BeginTransactionAsync(
                        CancellationToken.None);

            authorityClaim =
                await claimContext.Database
                    .SqlQuery<int>($"""
                    SELECT 1 AS [Value]
                    FROM [Decisions]
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Id] = {decision.Id}
                      AND [Revision] =
                          {observedRevision.Value}
                      AND [SupersededByDecisionId]
                          IS NULL
                    """)
                    .SingleOrDefaultAsync(
                        CancellationToken.None);

            await transaction.RollbackAsync(
                CancellationToken.None);
        }

        // Assert
        authorityClaim
            .Should()
            .Be(0);

        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision persistedDecision =
            await verificationContext
                .Set<Decision>()
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        decision.Id,
                    CancellationToken.None);

        persistedDecision.Revision
            .Should()
            .Be(observedRevision);

        persistedDecision.SupersededByDecisionId
            .Should()
            .Be(successorDecisionId);
    }

    [Fact]
    public async Task ReadLockAuthorityClaim_Should_HoldAuthority_ThroughApplicationCommit()
    {
        // Arrange
        Participation participation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New())
            .Value!;

        Decision decision =
            Decision.CreateParticipationClassification(
                participation.ParticipationId,
                ParticipationClassificationSnapshot.Create(
                    participation.ActivityReference,
                    participation.AtmacaCardId,
                    participation.Status,
                    participation.Condition,
                    participation.JoinedAt,
                    participation.LeftAt),
                ParticipationClassificationEffect.Present());

        await using (
            var seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            seedContext.Set<Decision>().Add(
                decision);

            await seedContext.SaveChangesAsync(
                CancellationToken.None);
        }

        await using var applicationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        await using var competingContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision applicationDecision =
            await applicationContext
                .Set<Decision>()
                .SingleAsync(
                    candidate =>
                        candidate.Id == decision.Id,
                    CancellationToken.None);

        Participation applicationParticipation =
            await applicationContext.Participations
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        participation.Id,
                    CancellationToken.None);

        Decision competingDecision =
            await competingContext
                .Set<Decision>()
                .SingleAsync(
                    candidate =>
                        candidate.Id == decision.Id,
                    CancellationToken.None);

        DecisionRevision appliedRevision =
            applicationDecision.Revision;

        await using var applicationTransaction =
            await applicationContext.Database
                .BeginTransactionAsync(
                    CancellationToken.None);

        // T1 claims exact authority and holds the lock.
        int authorityClaim =
            await applicationContext.Database
                .SqlQuery<int>($"""
                SELECT 1 AS [Value]
                FROM [Decisions]
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {decision.Id}
                  AND [Revision] =
                      {appliedRevision.Value}
                  AND [SupersededByDecisionId]
                      IS NULL
                """)
                .SingleOrDefaultAsync(
                    CancellationToken.None);

        authorityClaim
            .Should()
            .Be(1);

        // T2 attempts to mutate authority after T1's claim.
        ParticipationClassificationSnapshot
            changedSnapshot =
                ParticipationClassificationSnapshot.Create(
                    participation.ActivityReference,
                    participation.AtmacaCardId,
                    ParticipationStatus.Absent,
                    participation.Condition,
                    participation.JoinedAt,
                    participation.LeftAt);

        competingDecision.ReEvaluate(
            changedSnapshot,
            ParticipationClassificationEffect.Absent());

        Task competingSaveTask =
            competingContext.SaveChangesAsync(
                CancellationToken.None);

        // The competing authority mutation must not complete
        // while T1 still owns the authority lock.
        Task completedTask =
            await Task.WhenAny(
                competingSaveTask,
                Task.Delay(300));

        completedTask
            .Should()
            .NotBeSameAs(competingSaveTask);

        // T1 applies the authorized effect and provenance.
        applicationParticipation.MarkPresent();

        DecisionApplication decisionApplication =
            DecisionApplication.Create(
                applicationDecision.DecisionId,
                applicationDecision.Target,
                appliedRevision,
                new DateTimeOffset(
                    2026,
                    8,
                    31,
                    9,
                    0,
                    0,
                    TimeSpan.Zero));

        applicationContext
            .Set<DecisionApplication>()
            .Add(decisionApplication);

        await applicationContext.SaveChangesAsync(
            CancellationToken.None);

        await applicationTransaction.CommitAsync(
            CancellationToken.None);

        // Only after T1 commits may T2's authority mutation complete.
        await competingSaveTask;

        // Assert
        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision persistedDecision =
            await verificationContext
                .Set<Decision>()
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id == decision.Id,
                    CancellationToken.None);

        Participation persistedParticipation =
            await verificationContext.Participations
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        participation.Id,
                    CancellationToken.None);

        DecisionApplication persistedApplication =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.DecisionId ==
                        decision.DecisionId,
                    CancellationToken.None);

        persistedDecision.Revision
            .Should()
            .Be(appliedRevision.Next());

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.Present);

        persistedApplication.AppliedDecisionRevision
            .Should()
            .Be(appliedRevision);
    }
}
