using FluentAssertions;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionReEvaluationTests
{
    [Fact]
    public void ReEvaluate_Should_AdvanceRevisionAndReplaceApplicationRelevantSemantics()
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

        DecisionId originalDecisionId =
            decision.DecisionId;

        DecisionTargetReference originalTarget =
            decision.Target;

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
        decision.DecisionId
            .Should()
            .Be(originalDecisionId);

        decision.Target
            .Should()
            .Be(originalTarget);

        decision.Revision
            .Should()
            .Be(DecisionRevision.Initial.Next());

        decision.Snapshot
            .Should()
            .Be(revisedSnapshot);

        decision.Effect
            .Should()
            .Be(revisedEffect);
    }

    [Fact]
    public void ReEvaluate_Should_NotAdvanceRevision_WhenSemanticsAreUnchanged()
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

        DecisionRevision originalRevision =
            decision.Revision;

        // Act
        decision.ReEvaluate(
            snapshot,
            effect);

        // Assert
        decision.Revision
            .Should()
            .Be(originalRevision);

        decision.Snapshot
            .Should()
            .Be(snapshot);

        decision.Effect
            .Should()
            .Be(effect);
    }

    [Fact]
    public void ReEvaluate_Should_NotAdvanceRevision_WhenEquivalentSemanticsAreRecreated()
    {
        // Arrange
        ParticipationId participationId =
            ParticipationId.New();

        TrainingId trainingId =
            TrainingId.New();

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ActivityReference initialActivityReference =
            ActivityReference.ForTraining(
                trainingId);

        ParticipationClassificationSnapshot initialSnapshot =
            ParticipationClassificationSnapshot.Create(
                initialActivityReference,
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

        DecisionRevision originalRevision =
            decision.Revision;

        ActivityReference equivalentActivityReference =
            ActivityReference.ForTraining(
                trainingId);

        ParticipationClassificationSnapshot equivalentSnapshot =
            ParticipationClassificationSnapshot.Create(
                equivalentActivityReference,
                atmacaCardId,
                ParticipationStatus.NotRecorded,
                null,
                null,
                null);

        ParticipationClassificationEffect equivalentEffect =
            ParticipationClassificationEffect.Present();

        // Act
        decision.ReEvaluate(
            equivalentSnapshot,
            equivalentEffect);

        // Assert
        decision.Revision
            .Should()
            .Be(originalRevision);

        decision.Snapshot
            .Should()
            .Be(initialSnapshot);

        decision.Effect
            .Should()
            .Be(initialEffect);
    }

    [Fact]
    public void ReEvaluate_Should_AdvanceRevision_WhenOnlySnapshotChanges()
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

        ParticipationClassificationEffect effect =
            ParticipationClassificationEffect.Present();

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                initialSnapshot,
                effect);

        DecisionRevision originalRevision =
            decision.Revision;

        ParticipationClassificationSnapshot revisedSnapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.Present,
                null,
                null,
                null);

        // Act
        decision.ReEvaluate(
            revisedSnapshot,
            effect);

        // Assert
        decision.Revision
            .Should()
            .Be(originalRevision.Next());

        decision.Snapshot
            .Should()
            .Be(revisedSnapshot);

        decision.Effect
            .Should()
            .Be(effect);
    }

    [Fact]
    public void ReEvaluate_Should_AdvanceRevision_WhenOnlyEffectChanges()
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

        ParticipationClassificationEffect initialEffect =
            ParticipationClassificationEffect.Present();

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                snapshot,
                initialEffect);

        DecisionRevision originalRevision =
            decision.Revision;

        ParticipationClassificationEffect revisedEffect =
            ParticipationClassificationEffect.Absent();

        // Act
        decision.ReEvaluate(
            snapshot,
            revisedEffect);

        // Assert
        decision.Revision
            .Should()
            .Be(originalRevision.Next());

        decision.Snapshot
            .Should()
            .Be(snapshot);

        decision.Effect
            .Should()
            .Be(revisedEffect);
    }

    [Fact]
    public void ReEvaluate_Should_AdvanceSequentiallyAcrossMultipleSemanticChanges()
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

        DecisionId originalDecisionId =
            decision.DecisionId;

        DecisionTargetReference originalTarget =
            decision.Target;

        ParticipationClassificationSnapshot secondSnapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.Present,
                null,
                null,
                null);

        ParticipationClassificationEffect secondEffect =
            ParticipationClassificationEffect.Absent();

        ParticipationClassificationSnapshot thirdSnapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.Absent,
                null,
                null,
                null);

        ParticipationClassificationEffect thirdEffect =
            ParticipationClassificationEffect.Present();

        // Act
        decision.ReEvaluate(
            secondSnapshot,
            secondEffect);

        DecisionRevision secondRevision =
            decision.Revision;

        decision.ReEvaluate(
            thirdSnapshot,
            thirdEffect);

        // Assert
        secondRevision
            .Should()
            .Be(DecisionRevision.Initial.Next());

        decision.Revision
            .Should()
            .Be(DecisionRevision.Initial.Next().Next());

        decision.DecisionId
            .Should()
            .Be(originalDecisionId);

        decision.Target
            .Should()
            .Be(originalTarget);

        decision.Snapshot
            .Should()
            .Be(thirdSnapshot);

        decision.Effect
            .Should()
            .Be(thirdEffect);
    }
}
