using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionApplicationAtomicPersistenceTests
{
    [Fact]
    public async Task SaveChangesAsync_Should_CommitParticipationAndDecisionApplication_Atomically()
    {
        // Arrange — establish committed Participation state.
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

        ParticipationId participationId =
            participation.ParticipationId;

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            await seedContext.SaveChangesAsync();
        }

        DecisionId decisionId =
            DecisionId.New();

        DecisionRevision appliedRevision =
            DecisionRevision.From(1);

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                28,
                12,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplication decisionApplication =
            DecisionApplication.Create(
                decisionId,
                DecisionTargetReference.ForParticipation(
                    participationId),
                appliedRevision,
                appliedAtUtc);

        DecisionApplicationId decisionApplicationId =
            decisionApplication.DecisionApplicationId;

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Participation persistedParticipation =
                await writeContext.Participations
                    .SingleAsync(
                        item =>
                            item.Id ==
                            participationId.Value);

            persistedParticipation
                .MarkPresent()
                .IsSuccess
                .Should()
                .BeTrue();

            writeContext
                .Set<DecisionApplication>()
                .Add(decisionApplication);

            UnitOfWork unitOfWork =
                new(writeContext);

            // Act — exactly one persistence boundary.
            int affectedRows =
                await unitOfWork.SaveChangesAsync();

            // Assert
            affectedRows
                .Should()
                .BeGreaterThanOrEqualTo(2);
        }

        // Assert — a fresh DbContext proves durable SQL state.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? persisted =
            await verificationContext.Participations
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        participationId.Value);

        DecisionApplication? persistedApplication =
            await verificationContext
                .Set<DecisionApplication>()
                .FindAsync(
                    [decisionApplicationId.Value]);

        persisted
            .Should()
            .NotBeNull();

        persisted!.Status
            .Should()
            .Be(ParticipationStatus.Present);

        persistedApplication
            .Should()
            .NotBeNull();

        persistedApplication!.DecisionId
            .Should()
            .Be(decisionId);

        persistedApplication.Target
            .Should()
            .Be(
                DecisionTargetReference.ForParticipation(
                    participationId));

        persistedApplication.AppliedDecisionRevision
            .Should()
            .Be(appliedRevision);

        persistedApplication.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_RollBackParticipationAndDecisionApplication_WhenAnyTrackedChangeFails()
    {
        // Arrange — committed Participation that will be updated.
        ActivityReference targetActivityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId targetAtmacaCardId =
            AtmacaCardId.New();

        Participation targetParticipation =
            Participation.Create(
                targetActivityReference,
                targetAtmacaCardId)
            .Value!;

        ParticipationId targetParticipationId =
            targetParticipation.ParticipationId;

        // This committed row establishes the uniqueness conflict.
        ActivityReference duplicateActivityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId duplicateAtmacaCardId =
            AtmacaCardId.New();

        Participation existingParticipation =
            Participation.Create(
                duplicateActivityReference,
                duplicateAtmacaCardId)
            .Value!;

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                targetParticipation,
                existingParticipation);

            await seedContext.SaveChangesAsync();
        }

        DecisionApplication decisionApplication =
            DecisionApplication.Create(
                DecisionId.New(),
                DecisionTargetReference.ForParticipation(
                    targetParticipationId),
                DecisionRevision.Initial,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    12,
                    30,
                    0,
                    TimeSpan.Zero));

        DecisionApplicationId decisionApplicationId =
            decisionApplication.DecisionApplicationId;

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Participation persistedTarget =
                await writeContext.Participations
                    .SingleAsync(
                        item =>
                            item.Id ==
                            targetParticipationId.Value);

            persistedTarget
                .MarkPresent()
                .IsSuccess
                .Should()
                .BeTrue();

            writeContext
                .Set<DecisionApplication>()
                .Add(decisionApplication);

            // Valid domain entity, but invalid relational state:
            // same AtmacaCardId + ActivityReference as the
            // already committed Participation.
            Participation duplicateParticipation =
                Participation.Create(
                    duplicateActivityReference,
                    duplicateAtmacaCardId)
                .Value!;

            writeContext.Participations.Add(
                duplicateParticipation);

            UnitOfWork unitOfWork =
                new(writeContext);

            // Act
            Func<Task> action =
                async () =>
                    await unitOfWork.SaveChangesAsync();

            // Assert — one failed SQL command must reject
            // the entire SaveChanges transaction.
            await action
                .Should()
                .ThrowAsync<DbUpdateException>();
        }

        // Assert — use a fresh DbContext to inspect committed SQL state.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? persistedTargetAfterFailure =
            await verificationContext.Participations
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        targetParticipationId.Value);

        DecisionApplication? persistedApplication =
            await verificationContext
                .Set<DecisionApplication>()
                .FindAsync(
                    [decisionApplicationId.Value]);

        int duplicateCount =
            await verificationContext.Participations
                .CountAsync(
                    item =>
                        item.AtmacaCardId ==
                            duplicateAtmacaCardId &&
                        item.ActivityReference ==
                            duplicateActivityReference);

        persistedTargetAfterFailure
            .Should()
            .NotBeNull();

        // The UPDATE must have rolled back.
        persistedTargetAfterFailure!.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        // The provenance INSERT must also have rolled back.
        persistedApplication
            .Should()
            .BeNull();

        // Only the originally committed row may remain.
        duplicateCount
            .Should()
            .Be(1);
    }
}
