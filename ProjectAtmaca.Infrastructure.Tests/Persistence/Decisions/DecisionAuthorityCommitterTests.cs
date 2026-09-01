using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Decisions.ApplyParticipationClassification;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionAuthorityCommitterTests
{
    [Fact]
    public async Task CommitAsync_Should_CommitTrackedWrites_WhenExactAuthorityStillExists()
    {
        // Arrange
        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

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
                    operationId,
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
        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

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

            DecisionAuthorityCommitOutcome outcome =
                await committer.CommitAsync(
                    operationId,
                    loadedDecision.DecisionId,
                    staleRevision,
                    CancellationToken.None);

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
        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

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
                    operationId,
                    loadedDecision.DecisionId,
                    staleRevision,
                    CancellationToken.None);

            outcome
                .Should()
                .Be(
                    DecisionAuthorityCommitOutcome.AuthorityLost);

            await applicationContext.SaveChangesAsync(
                CancellationToken.None);
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
        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

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

            Func<Task> act =
                async () =>
                    await committer.CommitAsync(
                        operationId,
                        loadedDecision.DecisionId,
                        loadedDecision.Revision,
                        CancellationToken.None);

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

    [Fact]
    public async Task CommitAsync_Should_CommitParticipationDecisionApplicationAndOperation_WhenExactAuthorityStillExists()
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

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                9,
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

            loadedParticipation
                .MarkPresent()
                .IsSuccess
                .Should()
                .BeTrue();

            DecisionApplication decisionApplication =
                DecisionApplication.Create(
                    loadedDecision.DecisionId,
                    loadedDecision.Target,
                    loadedDecision.Revision,
                    appliedAtUtc);

            applicationContext
                .Set<DecisionApplication>()
                .Add(decisionApplication);

            DecisionApplicationOperation operation =
                new(
                    operationId,
                    loadedDecision.DecisionId,
                    loadedDecision.Revision,
                    appliedAtUtc);

            applicationContext
                .Set<DecisionApplicationOperation>()
                .Add(operation);

            var committer =
                new DecisionAuthorityCommitter(
                    applicationContext);

            DecisionAuthorityCommitOutcome outcome =
                await committer.CommitAsync(
                    operationId,
                    loadedDecision.DecisionId,
                    loadedDecision.Revision,
                    CancellationToken.None);

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

        DecisionApplicationOperation? persistedOperation =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OperationId ==
                        operationId,
                    CancellationToken.None);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.Present);

        persistedApplication.DecisionId
            .Should()
            .Be(decision.DecisionId);

        persistedApplication.Target
            .Should()
            .Be(decision.Target);

        persistedApplication.AppliedDecisionRevision
            .Should()
            .Be(decision.Revision);

        persistedApplication.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);

        persistedOperation
            .Should()
            .NotBeNull();

        persistedOperation!.OperationId
            .Should()
            .Be(operationId);

        persistedOperation.DecisionId
            .Should()
            .Be(decision.DecisionId);

        persistedOperation.DecisionRevision
            .Should()
            .Be(decision.Revision);

        persistedOperation.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }

    [Fact]
    public async Task CommitAsync_Should_RollBackParticipationDecisionApplicationAndOperation_WhenSaveChangesFails()
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

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                9,
                30,
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

            loadedParticipation
                .MarkPresent()
                .IsSuccess
                .Should()
                .BeTrue();

            DecisionApplication decisionApplication =
                DecisionApplication.Create(
                    loadedDecision.DecisionId,
                    loadedDecision.Target,
                    loadedDecision.Revision,
                    appliedAtUtc);

            applicationContext
                .Set<DecisionApplication>()
                .Add(decisionApplication);

            DecisionApplicationOperation operation =
                new(
                    operationId,
                    loadedDecision.DecisionId,
                    loadedDecision.Revision,
                    appliedAtUtc);

            applicationContext
                .Set<DecisionApplicationOperation>()
                .Add(operation);

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

            Func<Task> act =
                async () =>
                    await committer.CommitAsync(
                        operationId,
                        loadedDecision.DecisionId,
                        loadedDecision.Revision,
                        CancellationToken.None);

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

        DecisionApplicationOperation? persistedOperation =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OperationId ==
                        operationId,
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

        persistedOperation
            .Should()
            .BeNull();

        participationCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task CommitAsync_Should_NotAllowRejectedOperation_ToPersistOnLaterSaveChanges()
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

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
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

            DecisionRevision staleRevision =
                loadedDecision.Revision;

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

            loadedParticipation
                .MarkPresent()
                .IsSuccess
                .Should()
                .BeTrue();

            DecisionApplication staleApplication =
                DecisionApplication.Create(
                    loadedDecision.DecisionId,
                    loadedDecision.Target,
                    staleRevision,
                    appliedAtUtc);

            applicationContext
                .Set<DecisionApplication>()
                .Add(staleApplication);

            DecisionApplicationOperation staleOperation =
                new(
                    operationId,
                    loadedDecision.DecisionId,
                    staleRevision,
                    appliedAtUtc);

            applicationContext
                .Set<DecisionApplicationOperation>()
                .Add(staleOperation);

            var committer =
                new DecisionAuthorityCommitter(
                    applicationContext);

            DecisionAuthorityCommitOutcome outcome =
                await committer.CommitAsync(
                    operationId,
                    loadedDecision.DecisionId,
                    staleRevision,
                    CancellationToken.None);

            outcome
                .Should()
                .Be(
                    DecisionAuthorityCommitOutcome.AuthorityLost);

            await applicationContext.SaveChangesAsync(
                CancellationToken.None);
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

        DecisionApplicationOperation? persistedOperation =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OperationId ==
                        operationId,
                    CancellationToken.None);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        persistedApplications
            .Should()
            .BeEmpty();

        persistedOperation
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task
    DecisionApplicationOperation_Should_RejectDuplicateOperationId_AtRelationalBoundary()
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
                10,
                30,
                0,
                TimeSpan.Zero);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext
                .Set<DecisionApplicationOperation>()
                .Add(
                    new DecisionApplicationOperation(
                        operationId,
                        decisionId,
                        revision,
                        appliedAtUtc));

            await seedContext.SaveChangesAsync(
                CancellationToken.None);
        }

        await using ProjectAtmacaDbContext duplicateContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        duplicateContext
            .Set<DecisionApplicationOperation>()
            .Add(
                new DecisionApplicationOperation(
                    operationId,
                    decisionId,
                    revision,
                    appliedAtUtc));

        // Act
        Func<Task> act =
            async () =>
                await duplicateContext.SaveChangesAsync(
                    CancellationToken.None);

        // Assert
        await act
            .Should()
            .ThrowAsync<DbUpdateException>();

        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        int operationCount =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .CountAsync(
                    candidate =>
                        candidate.OperationId ==
                        operationId,
                    CancellationToken.None);

        operationCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        CommitAsync_Should_ReturnOperationAlreadyExists_WhenOperationIdIsAlreadyDurable()
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

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                14,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplicationOperation existingOperation =
            new(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        await using (
            var seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            seedContext.Set<Decision>().Add(
                decision);

            seedContext
                .Set<DecisionApplicationOperation>()
                .Add(existingOperation);

            await seedContext.SaveChangesAsync(
                CancellationToken.None);
        }

        await using var applicationContext =
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
                        candidate.Id == participation.Id,
                    CancellationToken.None);

        applicationParticipation.MarkPresent();

        DecisionApplication decisionApplication =
            DecisionApplication.Create(
                applicationDecision.DecisionId,
                applicationDecision.Target,
                applicationDecision.Revision,
                appliedAtUtc);

        applicationContext
            .Set<DecisionApplication>()
            .Add(decisionApplication);

        DecisionApplicationOperation duplicateOperation =
            new(
                operationId,
                applicationDecision.DecisionId,
                applicationDecision.Revision,
                appliedAtUtc);

        applicationContext
            .Set<DecisionApplicationOperation>()
            .Add(duplicateOperation);

        var committer =
            new DecisionAuthorityCommitter(
                applicationContext);

        DecisionAuthorityCommitOutcome outcome =
            await committer.CommitAsync(
                operationId,
                applicationDecision.DecisionId,
                applicationDecision.Revision,
                CancellationToken.None);

        outcome
            .Should()
            .Be(
                DecisionAuthorityCommitOutcome
                    .OperationAlreadyExists);

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

        DecisionApplication? persistedApplication =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id ==
                        decisionApplication.Id,
                    CancellationToken.None);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        persistedApplication
            .Should()
            .BeNull();

        (
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .CountAsync(
                    candidate =>
                        candidate.OperationId == operationId,
                    CancellationToken.None)
        )
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        CommitAsync_Should_NotAllowDuplicateOperationWrites_ToPersistOnLaterSaveChanges()
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

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                14,
                30,
                0,
                TimeSpan.Zero);

        DecisionApplicationOperation existingOperation =
            new(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            seedContext
                .Set<Decision>()
                .Add(decision);

            seedContext
                .Set<DecisionApplicationOperation>()
                .Add(existingOperation);

            await seedContext.SaveChangesAsync(
                CancellationToken.None);
        }

        await using ProjectAtmacaDbContext applicationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

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
                appliedAtUtc);

        applicationContext
            .Set<DecisionApplication>()
            .Add(decisionApplication);

        DecisionApplicationOperation duplicateOperation =
            new(
                operationId,
                loadedDecision.DecisionId,
                loadedDecision.Revision,
                appliedAtUtc);

        applicationContext
            .Set<DecisionApplicationOperation>()
            .Add(duplicateOperation);

        DecisionAuthorityCommitter committer =
            new(applicationContext);

        DecisionAuthorityCommitOutcome outcome =
            await committer.CommitAsync(
                operationId,
                loadedDecision.DecisionId,
                loadedDecision.Revision,
                CancellationToken.None);

        outcome
            .Should()
            .Be(
                DecisionAuthorityCommitOutcome
                    .OperationAlreadyExists);

        await applicationContext.SaveChangesAsync(
            CancellationToken.None);

        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation persistedParticipation =
            await verificationContext.Participations
                .AsNoTracking()
                .SingleAsync(
                    candidate =>
                        candidate.Id == participation.Id,
                    CancellationToken.None);

        DecisionApplication? persistedApplication =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id ==
                        decisionApplication.Id,
                    CancellationToken.None);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        persistedApplication
            .Should()
            .BeNull();

        (
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .CountAsync(
                    candidate =>
                        candidate.OperationId == operationId,
                    CancellationToken.None)
        )
            .Should()
            .Be(1);
    }
}
