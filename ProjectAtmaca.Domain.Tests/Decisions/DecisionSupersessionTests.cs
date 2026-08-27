using FluentAssertions;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionSupersessionTests
{
    [Fact]
    public void SupersedeBy_Should_RecordSuccessorWithoutChangingCurrentDecisionSemantics()
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

        DecisionId originalDecisionId =
            decision.DecisionId;

        DecisionRevision originalRevision =
            decision.Revision;

        DecisionTargetReference originalTarget =
            decision.Target;

        DecisionId successorDecisionId =
            DecisionId.New();

        // Act
        decision.SupersedeBy(
            successorDecisionId);

        // Assert
        decision.SupersededByDecisionId
            .Should()
            .Be(successorDecisionId);

        decision.DecisionId
            .Should()
            .Be(originalDecisionId);

        decision.Revision
            .Should()
            .Be(originalRevision);

        decision.Target
            .Should()
            .Be(originalTarget);

        decision.Snapshot
            .Should()
            .Be(snapshot);

        decision.Effect
            .Should()
            .Be(effect);
    }

    [Fact]
    public void SupersedeBy_Should_ThrowArgumentException_WhenSuccessorIsSameDecision()
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

        // Act
        Action act =
            () => decision.SupersedeBy(
                decision.DecisionId);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("successorDecisionId");
    }

    [Fact]
    public void SupersedeBy_Should_BeIdempotent_WhenSuccessorIsAlreadyRecorded()
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

        DecisionId successorDecisionId =
            DecisionId.New();

        decision.SupersedeBy(
            successorDecisionId);

        DecisionRevision revisionBeforeRepeatedCall =
            decision.Revision;

        // Act
        decision.SupersedeBy(
            successorDecisionId);

        // Assert
        decision.SupersededByDecisionId
            .Should()
            .Be(successorDecisionId);

        decision.Revision
            .Should()
            .Be(revisionBeforeRepeatedCall);

        decision.Snapshot
            .Should()
            .Be(snapshot);

        decision.Effect
            .Should()
            .Be(effect);
    }

    [Fact]
    public void SupersedeBy_Should_ThrowInvalidOperationException_WhenDifferentSuccessorIsAlreadyRecorded()
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

        DecisionId originalSuccessorDecisionId =
            DecisionId.New();

        DecisionId differentSuccessorDecisionId =
            DecisionId.New();

        decision.SupersedeBy(
            originalSuccessorDecisionId);

        // Act
        Action act =
            () => decision.SupersedeBy(
                differentSuccessorDecisionId);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        decision.SupersededByDecisionId
            .Should()
            .Be(originalSuccessorDecisionId);
    }

    [Fact]
    public void ReEvaluate_Should_ThrowInvalidOperationException_WhenDecisionIsSuperseded()
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

        DecisionId successorDecisionId =
            DecisionId.New();

        decision.SupersedeBy(
            successorDecisionId);

        DecisionRevision revisionBeforeAttempt =
            decision.Revision;

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
        Action act =
            () => decision.ReEvaluate(
                revisedSnapshot,
                revisedEffect);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        decision.Revision
            .Should()
            .Be(revisionBeforeAttempt);

        decision.Snapshot
            .Should()
            .Be(initialSnapshot);

        decision.Effect
            .Should()
            .Be(initialEffect);

        decision.SupersededByDecisionId
            .Should()
            .Be(successorDecisionId);
    }
}
