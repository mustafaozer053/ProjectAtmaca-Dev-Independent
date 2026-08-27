using FluentAssertions;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionCreationTests
{
    [Fact]
    public void CreateParticipationClassification_Should_CreateInitialDecision()
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

        // Act
        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                snapshot,
                effect);

        // Assert
        decision.DecisionId.Value
            .Should()
            .NotBe(Guid.Empty);

        decision.Revision
            .Should()
            .Be(DecisionRevision.Initial);

        decision.Target
            .Should()
            .Be(
                DecisionTargetReference.ForParticipation(
                    participationId));

        decision.Snapshot
            .Should()
            .Be(snapshot);

        decision.Effect
            .Should()
            .Be(effect);
    }
}
