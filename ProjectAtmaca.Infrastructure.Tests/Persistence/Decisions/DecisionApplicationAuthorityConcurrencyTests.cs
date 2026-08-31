using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
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
public sealed class DecisionApplicationAuthorityConcurrencyTests
{
    [Fact]
    public async Task CommitAsync_Should_RejectApplication_WhenDecisionRevisionBecameStale()
    {
        // Arrange — establish authoritative revision 1.
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

        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.NotRecorded,
                null,
                null,
                null);

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                snapshot,
                ParticipationClassificationEffect.Present());

        DecisionId decisionId =
            decision.DecisionId;

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

        // T1 — application reads and validates revision 1.
        await using ProjectAtmacaDbContext applicationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision staleDecision =
            await applicationContext
                .Set<Decision>()
                .SingleAsync(
                    item =>
                        item.Id ==
                        decisionId.Value);

        Participation targetParticipation =
            await applicationContext.Participations
                .SingleAsync(
                    item =>
                        item.Id ==
                        participationId.Value);

        staleDecision.Revision
            .Should()
            .Be(DecisionRevision.Initial);

        staleDecision.SupersededByDecisionId
            .Should()
            .BeNull();

        // T2 — authority changes after T1 validated it:
        // revision 1 becomes revision 2.
        await using (
            ProjectAtmacaDbContext authorityContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision authoritativeDecision =
                await authorityContext
                    .Set<Decision>()
                    .SingleAsync(
                        item =>
                            item.Id ==
                            decisionId.Value);

            ParticipationClassificationSnapshot
                revisedSnapshot =
                    ParticipationClassificationSnapshot.Create(
                        activityReference,
                        atmacaCardId,
                        ParticipationStatus.Present,
                        null,
                        null,
                        null);

            authoritativeDecision.ReEvaluate(
                revisedSnapshot,
                ParticipationClassificationEffect.Absent());

            await authorityContext.SaveChangesAsync();
        }

        // T1 — continues using the stale revision
        // that it already validated.
        targetParticipation
            .MarkPresent()
            .IsSuccess
            .Should()
            .BeTrue();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                29,
                9,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplication staleApplication =
            DecisionApplication.Create(
                staleDecision.DecisionId,
                staleDecision.Target,
                staleDecision.Revision,
                appliedAtUtc);

        DecisionApplicationId staleApplicationId =
            staleApplication.DecisionApplicationId;

        applicationContext
            .Set<DecisionApplication>()
            .Add(staleApplication);

        DecisionAuthorityCommitter committer =
            new(applicationContext);

        DecisionAuthorityCommitOutcome outcome =
            await committer.CommitAsync(
                staleDecision.DecisionId,
                staleDecision.Revision);

        outcome
            .Should()
            .Be(
                DecisionAuthorityCommitOutcome.AuthorityLost);

        // Assert — inspect durable SQL state
        // through a fresh DbContext.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision authoritativeAfterRace =
            await verificationContext
                .Set<Decision>()
                .SingleAsync(
                    item =>
                        item.Id ==
                        decisionId.Value);

        Participation participationAfterRace =
            await verificationContext.Participations
                .SingleAsync(
                    item =>
                        item.Id ==
                        participationId.Value);

        DecisionApplication? staleApplicationAfterRace =
            await verificationContext
                .Set<DecisionApplication>()
                .FindAsync(
                    [staleApplicationId.Value]);

        using (new AssertionScope())
        {
            authoritativeAfterRace.Revision
                .Should()
                .Be(
                    DecisionRevision.Initial.Next());

            participationAfterRace.Status
                .Should()
                .Be(
                    ParticipationStatus.NotRecorded,
                    "a stale Decision revision must not be allowed " +
                    "to change the target state");

            staleApplicationAfterRace
                .Should()
                .BeNull(
                    "an application based on a stale Decision revision " +
                    "must not become durable provenance");
        }
    }

    [Fact]
    public async Task CommitAsync_Should_RejectApplication_WhenDecisionWasSuperseded()
    {
        // Arrange — establish an authoritative,
        // non-superseded revision 1.
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

        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.NotRecorded,
                null,
                null,
                null);

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                snapshot,
                ParticipationClassificationEffect.Present());

        DecisionId decisionId =
            decision.DecisionId;

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

        // T1 — application reads and validates
        // revision 1 while Decision is authoritative.
        await using ProjectAtmacaDbContext applicationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision staleDecision =
            await applicationContext
                .Set<Decision>()
                .SingleAsync(
                    item =>
                        item.Id ==
                        decisionId.Value);

        Participation targetParticipation =
            await applicationContext.Participations
                .SingleAsync(
                    item =>
                        item.Id ==
                        participationId.Value);

        staleDecision.Revision
            .Should()
            .Be(DecisionRevision.Initial);

        staleDecision.SupersededByDecisionId
            .Should()
            .BeNull();

        // T2 — authority is lost through supersession.
        // Notice that the revision itself remains revision 1.
        DecisionId successorDecisionId =
            DecisionId.New();

        await using (
            ProjectAtmacaDbContext authorityContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Decision authoritativeDecision =
                await authorityContext
                    .Set<Decision>()
                    .SingleAsync(
                        item =>
                            item.Id ==
                            decisionId.Value);

            authoritativeDecision.SupersedeBy(
                successorDecisionId);

            await authorityContext.SaveChangesAsync();
        }

        // T1 — continues using the Decision instance
        // that was authoritative when it was read.
        targetParticipation
            .MarkPresent()
            .IsSuccess
            .Should()
            .BeTrue();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                29,
                9,
                30,
                0,
                TimeSpan.Zero);

        DecisionApplication staleApplication =
            DecisionApplication.Create(
                staleDecision.DecisionId,
                staleDecision.Target,
                staleDecision.Revision,
                appliedAtUtc);

        DecisionApplicationId staleApplicationId =
            staleApplication.DecisionApplicationId;

        applicationContext
            .Set<DecisionApplication>()
            .Add(staleApplication);

        DecisionAuthorityCommitter committer =
            new(applicationContext);

        DecisionAuthorityCommitOutcome outcome =
            await committer.CommitAsync(
                staleDecision.DecisionId,
                staleDecision.Revision);

        outcome
            .Should()
            .Be(
                DecisionAuthorityCommitOutcome.AuthorityLost);

        // Assert — inspect durable SQL state
        // through a fresh DbContext.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Decision authoritativeAfterRace =
            await verificationContext
                .Set<Decision>()
                .SingleAsync(
                    item =>
                        item.Id ==
                        decisionId.Value);

        Participation participationAfterRace =
            await verificationContext.Participations
                .SingleAsync(
                    item =>
                        item.Id ==
                        participationId.Value);

        DecisionApplication? staleApplicationAfterRace =
            await verificationContext
                .Set<DecisionApplication>()
                .FindAsync(
                    [staleApplicationId.Value]);

        using (new AssertionScope())
        {
            authoritativeAfterRace.Revision
                .Should()
                .Be(
                    DecisionRevision.Initial,
                    "supersession does not require a revision change");

            authoritativeAfterRace.SupersededByDecisionId
                .Should()
                .Be(successorDecisionId);

            participationAfterRace.Status
                .Should()
                .Be(
                    ParticipationStatus.NotRecorded,
                    "a superseded Decision must not be allowed " +
                    "to change the target state");

            staleApplicationAfterRace
                .Should()
                .BeNull(
                    "an application based on a superseded Decision " +
                    "must not become durable provenance");
        }
    }
}