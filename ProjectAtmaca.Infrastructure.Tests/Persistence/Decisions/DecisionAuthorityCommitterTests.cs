using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionAuthorityCommitterTests
{
    [Fact]
    public async Task CommitAsync_Should_CommitTrackedWrites_WhenExactAuthorityStillExists()
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

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                31,
                10,
                0,
                0,
                TimeSpan.Zero);

        await using (
            var applicationContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision loadedDecision =
                await applicationContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id == decision.Id,
                        CancellationToken.None);

            Participation loadedParticipation =
                await applicationContext.Participations
                    .SingleAsync(
                        candidate =>
                            candidate.Id ==
                            participation.Id,
                        CancellationToken.None);

            loadedParticipation.MarkPresent();

            DecisionApplication decisionApplication =
                DecisionApplication.Create(
                    loadedDecision.DecisionId,
                    loadedDecision.Target,
                    loadedDecision.Revision,
                    appliedAtUtc);

            applicationContext
                .Set<DecisionApplication>()
                .Add(decisionApplication);

            var committer =
                new DecisionAuthorityCommitter(
                    applicationContext);

            // Act
            DecisionAuthorityCommitOutcome outcome =
                await committer.CommitAsync(
                    loadedDecision.DecisionId,
                    loadedDecision.Revision,
                    CancellationToken.None);

            // Assert
            outcome
                .Should()
                .Be(
                    DecisionAuthorityCommitOutcome.Committed);
        }

        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

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

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.Present);

        persistedApplication.AppliedDecisionRevision
            .Should()
            .Be(DecisionRevision.Initial);

        persistedApplication.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }

    [Fact]
    public async Task CommitAsync_Should_NotPersistTrackedWrites_WhenAuthorityWasLost()
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

        await using (
            var applicationContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision loadedDecision =
                await applicationContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id == decision.Id,
                        CancellationToken.None);

            Participation loadedParticipation =
                await applicationContext.Participations
                    .SingleAsync(
                        candidate =>
                            candidate.Id ==
                            participation.Id,
                        CancellationToken.None);

            DecisionRevision staleRevision =
                loadedDecision.Revision;

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
                        ParticipationClassificationSnapshot.Create(
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

            // These changes are now based on stale authority.
            loadedParticipation.MarkPresent();

            DecisionApplication staleApplication =
                DecisionApplication.Create(
                    loadedDecision.DecisionId,
                    loadedDecision.Target,
                    staleRevision,
                    new DateTimeOffset(
                        2026,
                        8,
                        31,
                        10,
                        30,
                        0,
                        TimeSpan.Zero));

            applicationContext
                .Set<DecisionApplication>()
                .Add(staleApplication);

            var committer =
                new DecisionAuthorityCommitter(
                    applicationContext);

            // Act
            DecisionAuthorityCommitOutcome outcome =
                await committer.CommitAsync(
                    loadedDecision.DecisionId,
                    staleRevision,
                    CancellationToken.None);

            // Assert
            outcome
                .Should()
                .Be(
                    DecisionAuthorityCommitOutcome.AuthorityLost);
        }

        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation persistedParticipation =
            await verificationContext.Participations
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        participation.Id,
                    CancellationToken.None);

        List<DecisionApplication> persistedApplications =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .Where(
                    candidate =>
                        candidate.DecisionId ==
                        decision.DecisionId)
                .ToListAsync(
                    CancellationToken.None);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        persistedApplications
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_Should_NotAllowRejectedWrites_ToPersistOnLaterSaveChanges()
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

        await using (
            var applicationContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision loadedDecision =
                await applicationContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id == decision.Id,
                        CancellationToken.None);

            Participation loadedParticipation =
                await applicationContext.Participations
                    .SingleAsync(
                        candidate =>
                            candidate.Id == participation.Id,
                        CancellationToken.None);

            DecisionRevision staleRevision =
                loadedDecision.Revision;

            // Another actor changes authority first.
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
                                candidate.Id == decision.Id,
                            CancellationToken.None);

                ParticipationClassificationSnapshot changedSnapshot =
                    ParticipationClassificationSnapshot.Create(
                        participation.ActivityReference,
                        participation.AtmacaCardId,
                        ParticipationStatus.Absent,
                        participation.Condition,
                        participation.JoinedAt,
                        participation.LeftAt);

                authoritativeDecision.ReEvaluate(
                    changedSnapshot,
                    ParticipationClassificationEffect.Absent());

                await authorityMutationContext.SaveChangesAsync(
                    CancellationToken.None);
            }

            // Pending writes based on stale authority.
            loadedParticipation.MarkPresent();

            DecisionApplication staleApplication =
                DecisionApplication.Create(
                    loadedDecision.DecisionId,
                    loadedDecision.Target,
                    staleRevision,
                    new DateTimeOffset(
                        2026,
                        8,
                        31,
                        11,
                        0,
                        0,
                        TimeSpan.Zero));

            applicationContext
                .Set<DecisionApplication>()
                .Add(staleApplication);

            var committer =
                new DecisionAuthorityCommitter(
                    applicationContext);

            DecisionAuthorityCommitOutcome outcome =
                await committer.CommitAsync(
                    loadedDecision.DecisionId,
                    staleRevision,
                    CancellationToken.None);

            outcome
                .Should()
                .Be(
                    DecisionAuthorityCommitOutcome.AuthorityLost);

            // Act
            //
            // Simulate accidental later persistence in the same
            // scoped DbContext after the rejected authority commit.
            await applicationContext.SaveChangesAsync(
                CancellationToken.None);
        }

        // Assert
        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation persistedParticipation =
            await verificationContext.Participations
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id == participation.Id,
                    CancellationToken.None);

        List<DecisionApplication> persistedApplications =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .Where(
                    candidate =>
                        candidate.DecisionId ==
                        decision.DecisionId)
                .ToListAsync(
                    CancellationToken.None);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        persistedApplications
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task CommitAsync_Should_RollBackProtectedWrites_WhenSaveChangesFails()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation participation =
            Participation.Create(
                activityReference,
                atmacaCardId)
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

        await using (
            var applicationContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision loadedDecision =
                await applicationContext
                    .Set<Decision>()
                    .SingleAsync(
                        candidate =>
                            candidate.Id == decision.Id,
                        CancellationToken.None);

            Participation loadedParticipation =
                await applicationContext.Participations
                    .SingleAsync(
                        candidate =>
                            candidate.Id == participation.Id,
                        CancellationToken.None);

            loadedParticipation.MarkPresent();

            DecisionApplication decisionApplication =
                DecisionApplication.Create(
                    loadedDecision.DecisionId,
                    loadedDecision.Target,
                    loadedDecision.Revision,
                    new DateTimeOffset(
                        2026,
                        8,
                        31,
                        11,
                        30,
                        0,
                        TimeSpan.Zero));

            applicationContext
                .Set<DecisionApplication>()
                .Add(decisionApplication);

            // Deliberately violate the unique Participation
            // constraint:
            // (AtmacaCardId, ActivityReference).
            Participation duplicateParticipation =
                Participation.Create(
                    activityReference,
                    atmacaCardId)
                .Value!;

            applicationContext.Participations.Add(
                duplicateParticipation);

            var committer =
                new DecisionAuthorityCommitter(
                    applicationContext);

            // Act
            Func<Task> act =
                async () =>
                    await committer.CommitAsync(
                        loadedDecision.DecisionId,
                        loadedDecision.Revision,
                        CancellationToken.None);

            // Assert
            await act
                .Should()
                .ThrowAsync<DbUpdateException>();
        }

        await using var verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation persistedParticipation =
            await verificationContext.Participations
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id == participation.Id,
                    CancellationToken.None);

        List<DecisionApplication> persistedApplications =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .Where(
                    candidate =>
                        candidate.DecisionId ==
                        decision.DecisionId)
                .ToListAsync(
                    CancellationToken.None);

        int participationCount =
            await verificationContext.Participations
                .AsNoTracking()
                .CountAsync(
                    candidate =>
                        candidate.AtmacaCardId ==
                            atmacaCardId
                        &&
                        candidate.ActivityReference ==
                            activityReference,
                    CancellationToken.None);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        persistedApplications
            .Should()
            .BeEmpty();

        participationCount
            .Should()
            .Be(1);
    }
}
