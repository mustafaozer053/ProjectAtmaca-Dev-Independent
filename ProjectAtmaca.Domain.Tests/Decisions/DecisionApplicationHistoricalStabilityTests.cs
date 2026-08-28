using FluentAssertions;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionApplicationHistoricalStabilityTests
{
    [Fact]
    public void AppliedRevision_Should_RemainStable_WhenDecisionIsReEvaluatedLater()
    {
        // Arrange
        ParticipationId participationId =
            ParticipationId.New();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationClassificationSnapshot initialSnapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.NotRecorded,
                null,
                null,
                null);

        ParticipationClassificationEffect initialEffect =
            ParticipationClassificationEffect.Present();

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                initialSnapshot,
                initialEffect);

        DecisionRevision appliedRevision =
            decision.Revision;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                27,
                12,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplication application =
            DecisionApplication.Create(
                decision.DecisionId,
                decision.Target,
                appliedRevision,
                appliedAtUtc);

        ParticipationClassificationSnapshot revisedSnapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.Present,
                null,
                null,
                null);

        ParticipationClassificationEffect revisedEffect =
            ParticipationClassificationEffect.Absent();

        // Act
        decision.ReEvaluate(
            revisedSnapshot,
            revisedEffect);

        // Assert
        decision.Revision
            .Should()
            .Be(appliedRevision.Next());

        application.AppliedDecisionRevision
            .Should()
            .Be(appliedRevision);

        application.DecisionId
            .Should()
            .Be(decision.DecisionId);

        application.Target
            .Should()
            .Be(decision.Target);
    }

    [Fact]
    public void SeparateApplications_Should_PreserveTheRevisionAppliedAtEachPointInTime()
    {
        // Arrange
        ParticipationId participationId =
            ParticipationId.New();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationClassificationSnapshot initialSnapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.NotRecorded,
                null,
                null,
                null);

        ParticipationClassificationEffect initialEffect =
            ParticipationClassificationEffect.Present();

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                initialSnapshot,
                initialEffect);

        DecisionRevision firstAppliedRevision =
            decision.Revision;

        DateTimeOffset firstAppliedAtUtc =
            new(
                2026,
                8,
                27,
                12,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplication firstApplication =
            DecisionApplication.Create(
                decision.DecisionId,
                decision.Target,
                firstAppliedRevision,
                firstAppliedAtUtc);

        ParticipationClassificationSnapshot revisedSnapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.Present,
                null,
                null,
                null);

        ParticipationClassificationEffect revisedEffect =
            ParticipationClassificationEffect.Absent();

        decision.ReEvaluate(
            revisedSnapshot,
            revisedEffect);

        DecisionRevision secondAppliedRevision =
            decision.Revision;

        DateTimeOffset secondAppliedAtUtc =
            new(
                2026,
                8,
                27,
                13,
                0,
                0,
                TimeSpan.Zero);

        // Act
        DecisionApplication secondApplication =
            DecisionApplication.Create(
                decision.DecisionId,
                decision.Target,
                secondAppliedRevision,
                secondAppliedAtUtc);

        // Assert
        firstApplication.DecisionApplicationId
            .Should()
            .NotBe(secondApplication.DecisionApplicationId);

        firstApplication.DecisionId
            .Should()
            .Be(secondApplication.DecisionId);

        firstApplication.Target
            .Should()
            .Be(secondApplication.Target);

        firstApplication.AppliedDecisionRevision
            .Should()
            .Be(DecisionRevision.Initial);

        secondApplication.AppliedDecisionRevision
            .Should()
            .Be(DecisionRevision.Initial.Next());

        firstApplication.AppliedAtUtc
            .Should()
            .Be(firstAppliedAtUtc);

        secondApplication.AppliedAtUtc
            .Should()
            .Be(secondAppliedAtUtc);
    }

    [Fact]
    public void ApplicationProvenance_Should_RemainStable_WhenDecisionIsSupersededLater()
    {
        // Arrange
        ParticipationId participationId =
            ParticipationId.New();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.NotRecorded,
                null,
                null,
                null);

        ParticipationClassificationEffect effect =
            ParticipationClassificationEffect.Present();

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                snapshot,
                effect);

        DecisionId appliedDecisionId =
            decision.DecisionId;

        DecisionTargetReference appliedTarget =
            decision.Target;

        DecisionRevision appliedRevision =
            decision.Revision;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                27,
                12,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplication application =
            DecisionApplication.Create(
                appliedDecisionId,
                appliedTarget,
                appliedRevision,
                appliedAtUtc);

        DecisionId successorDecisionId =
            DecisionId.New();

        // Act
        decision.SupersedeBy(
            successorDecisionId);

        // Assert
        decision.SupersededByDecisionId
            .Should()
            .Be(successorDecisionId);

        application.DecisionId
            .Should()
            .Be(appliedDecisionId);

        application.Target
            .Should()
            .Be(appliedTarget);

        application.AppliedDecisionRevision
            .Should()
            .Be(appliedRevision);

        application.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }
}
