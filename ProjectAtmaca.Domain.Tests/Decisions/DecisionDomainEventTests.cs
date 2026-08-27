using FluentAssertions;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.DomainEvents;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionDomainEventTests
{
    [Fact]
    public void SupersedeBy_Should_RaiseDecisionSupersededDomainEvent()
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

        decision.ClearDomainEvents();

        // Act
        decision.SupersedeBy(
            successorDecisionId);

        // Assert
        DecisionSupersededDomainEvent domainEvent =
            decision.DomainEvents
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<DecisionSupersededDomainEvent>()
                .Which;

        domainEvent.DecisionId
            .Should()
            .Be(decision.DecisionId);

        domainEvent.SuccessorDecisionId
            .Should()
            .Be(successorDecisionId);
    }

    [Fact]
    public void SupersedeBy_Should_NotRaiseAdditionalEvent_WhenSameSuccessorIsRepeated()
    {
        // Arrange
        Decision decision =
            CreateDecision();

        DecisionId successorDecisionId =
            DecisionId.New();

        decision.ClearDomainEvents();

        decision.SupersedeBy(
            successorDecisionId);

        decision.DomainEvents
            .Should()
            .ContainSingle();

        // Act
        decision.SupersedeBy(
            successorDecisionId);

        // Assert
        decision.DomainEvents
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void SupersedeBy_Should_NotRaiseAdditionalEvent_WhenDifferentSuccessorIsRejected()
    {
        // Arrange
        Decision decision =
            CreateDecision();

        DecisionId originalSuccessorDecisionId =
            DecisionId.New();

        DecisionId differentSuccessorDecisionId =
            DecisionId.New();

        decision.ClearDomainEvents();

        decision.SupersedeBy(
            originalSuccessorDecisionId);

        // Act
        Action act =
            () => decision.SupersedeBy(
                differentSuccessorDecisionId);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        decision.DomainEvents
            .Should()
            .ContainSingle();

        DecisionSupersededDomainEvent domainEvent =
            decision.DomainEvents
                .Single()
                .Should()
                .BeOfType<DecisionSupersededDomainEvent>()
                .Which;

        domainEvent.SuccessorDecisionId
            .Should()
            .Be(originalSuccessorDecisionId);
    }

    [Fact]
    public void SupersedeBy_Should_NotRaiseEvent_WhenSelfSupersessionIsRejected()
    {
        // Arrange
        Decision decision =
            CreateDecision();

        decision.ClearDomainEvents();

        // Act
        Action act =
            () => decision.SupersedeBy(
                decision.DecisionId);

        // Assert
        act.Should()
            .Throw<ArgumentException>();

        decision.DomainEvents
            .Should()
            .BeEmpty();
    }

    private static Decision CreateDecision()
    {
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

        return Decision.CreateParticipationClassification(
            participationId,
            snapshot,
            effect);
    }
}
