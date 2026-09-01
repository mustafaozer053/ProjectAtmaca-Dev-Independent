using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class
    DecisionAuthorityCommitterOperationConcurrencyTests
{
    [Fact]
    public async Task
    CommitAsync_Should_ReturnOperationAlreadyExists_WhenConcurrentOperationBecomesDurable()
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
                    activityReference,
                    atmacaCardId,
                    ParticipationStatus.NotRecorded,
                    null,
                    null,
                    null),
                ParticipationClassificationEffect.Present());

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                13,
                30,
                0,
                TimeSpan.Zero);

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

            await seedContext.SaveChangesAsync();
        }

        // T1 — hold the exact OperationId key range.
        await using ProjectAtmacaDbContext winnerContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        await using var winnerTransaction =
            await winnerContext.Database
                .BeginTransactionAsync();

        Guid? existingOperationId =
            await winnerContext.Database
                .SqlQuery<Guid?>($"""
                    SELECT [OperationId] AS [Value]
                    FROM [DecisionApplicationOperations]
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE [OperationId] = {operationId.Value}
                    """)
                .SingleOrDefaultAsync();

        existingOperationId
            .Should()
            .BeNull();

        // T2 — stage a protected write-set and enter the
        // real production committer while T1 owns O1.
        await using ProjectAtmacaDbContext loserContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision loserDecision =
            await loserContext
                .Set<Decision>()
                .SingleAsync(
                    item =>
                        item.Id ==
                        decision.Id);

        Participation loserParticipation =
            await loserContext.Participations
                .SingleAsync(
                    item =>
                        item.Id ==
                        participation.Id);

        loserParticipation
            .MarkPresent()
            .IsSuccess
            .Should()
            .BeTrue();

        DecisionApplication loserApplication =
            DecisionApplication.Create(
                loserDecision.DecisionId,
                loserDecision.Target,
                loserDecision.Revision,
                appliedAtUtc);

        loserContext
            .Set<DecisionApplication>()
            .Add(loserApplication);

        DecisionApplicationOperation loserOperation =
            new(
                operationId,
                loserDecision.DecisionId,
                loserDecision.Revision,
                appliedAtUtc);

        loserContext
            .Set<DecisionApplicationOperation>()
            .Add(loserOperation);

        DecisionAuthorityCommitter loserCommitter =
            new(loserContext);

        Task<DecisionAuthorityCommitOutcome> loserCommitTask =
            loserCommitter.CommitAsync(
                operationId,
                loserDecision.DecisionId,
                loserDecision.Revision);

        Task completedTask =
            await Task.WhenAny(
                loserCommitTask,
                Task.Delay(300));

        completedTask
            .Should()
            .NotBeSameAs(
                loserCommitTask,
                "the concurrent committer must serialize " +
                "behind the protected OperationId");

        // T1 wins O1 and makes it durable.
        DecisionApplicationOperation winnerOperation =
            new(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        winnerContext
            .Set<DecisionApplicationOperation>()
            .Add(winnerOperation);

        await winnerContext.SaveChangesAsync();

        await winnerTransaction.CommitAsync();

        DecisionAuthorityCommitOutcome loserOutcome =
            await loserCommitTask;

        loserOutcome
            .Should()
            .Be(
                DecisionAuthorityCommitOutcome
                    .OperationAlreadyExists);

        // Assert — loser protected writes must not leak.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation persistedParticipation =
            await verificationContext.Participations
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        participation.Id);

        DecisionApplication? persistedLoserApplication =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        loserApplication.Id);

        int operationCount =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .CountAsync(
                    item =>
                        item.OperationId ==
                        operationId);

        persistedParticipation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        persistedLoserApplication
            .Should()
            .BeNull();

        operationCount
            .Should()
            .Be(1);
    }
}
