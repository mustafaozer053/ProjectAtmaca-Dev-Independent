using FluentAssertions;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Participations.DomainEvents;
using ProjectAtmaca.Domain.Trainings;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Participations;

public sealed class ParticipationDomainEventTests
{
    [Fact]
    public void Create_Should_RaiseParticipationCreatedDomainEvent()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        // Act
        Result<Participation> result =
            Participation.Create(
                activityReference,
                atmacaCardId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        Participation participation =
            result.Value!;

        ParticipationCreatedDomainEvent domainEvent =
            participation.DomainEvents
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeOfType<ParticipationCreatedDomainEvent>()
                .Which;

        domainEvent.ParticipationId.Should()
            .Be(participation.ParticipationId);

        domainEvent.ActivityReference.Should()
            .Be(activityReference);

        domainEvent.AtmacaCardId.Should()
            .Be(atmacaCardId);
    }

    [Fact]
    public void MarkPresent_Should_RaiseParticipationMarkedPresentDomainEvent()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.MarkPresent();

        // Assert
        result.IsSuccess.Should().BeTrue();

        participation.Status.Should()
            .Be(ParticipationStatus.Present);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore + 1);

        ParticipationMarkedPresentDomainEvent domainEvent =
            participation.DomainEvents
                .Should()
                .ContainSingle(e =>
                    e is ParticipationMarkedPresentDomainEvent)
                .Which
                .Should()
                .BeOfType<ParticipationMarkedPresentDomainEvent>()
                .Which;

        domainEvent.ParticipationId.Should()
            .Be(participation.ParticipationId);
    }

    [Fact]
    public void MarkPresent_Should_NotRaiseNewDomainEvent_When_SemanticStateIsUnchanged()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        Result firstResult =
            participation.MarkPresent();

        firstResult.IsSuccess.Should().BeTrue();

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result secondResult =
            participation.MarkPresent();

        // Assert
        secondResult.IsSuccess.Should().BeTrue();

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore);

        participation.DomainEvents
            .Should()
            .ContainSingle(e =>
                e is ParticipationMarkedPresentDomainEvent);
    }

    [Fact]
    public void MarkPresent_Should_NotRaiseDomainEvent_When_ClassificationCorrectionIsRequired()
    {
        // Arrange
        Participation participation =
            CreateAbsentBtaParticipation();

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.MarkPresent();

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ClassificationCorrectionRequired);

        participation.Status.Should()
            .Be(ParticipationStatus.Absent);

        participation.Condition.Should()
            .Be(ParticipationCondition.Bta);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore);

        participation.DomainEvents
            .Should()
            .NotContain(e =>
                e is ParticipationMarkedPresentDomainEvent);
    }

    private static Participation CreateNotRecordedParticipation()
    {
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        return Participation.Create(
            activityReference,
            atmacaCardId).Value!;
    }

    private static Participation CreateAbsentBtaParticipation()
    {
        Participation participation =
            CreateNotRecordedParticipation();

        Result result =
            participation.MarkAbsent(
                ParticipationCondition.Bta);

        result.IsSuccess.Should().BeTrue();

        return participation;
    }
}