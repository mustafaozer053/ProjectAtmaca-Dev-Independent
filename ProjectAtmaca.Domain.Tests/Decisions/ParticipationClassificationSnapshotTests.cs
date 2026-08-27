using FluentAssertions;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class ParticipationClassificationSnapshotTests
{
    [Fact]
    public void Create_Should_PreserveActivityReferenceAndStatus()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        ParticipationStatus status =
            ParticipationStatus.NotRecorded;

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        // Act
        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                status,
                null,
                null,
                null);

        // Assert
        snapshot.ActivityReference
            .Should()
            .Be(activityReference);

        snapshot.Status
            .Should()
            .Be(status);
    }

    [Fact]
    public void Create_Should_PreserveAtmacaCardIdentity()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationStatus status =
            ParticipationStatus.NotRecorded;

        // Act
        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                status,
                null,
                null,
                null);

        // Assert
        snapshot.AtmacaCardId
            .Should()
            .Be(atmacaCardId);
    }

    [Fact]
    public void Create_Should_PreserveClassificationApplicabilityFacts()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationStatus status =
            ParticipationStatus.Present;

        ParticipationCondition? condition =
            null;

        DateTimeOffset? joinedAt =
            new DateTimeOffset(
                2026,
                8,
                27,
                10,
                15,
                0,
                TimeSpan.Zero);

        DateTimeOffset? leftAt =
            new DateTimeOffset(
                2026,
                8,
                27,
                11,
                45,
                0,
                TimeSpan.Zero);

        // Act
        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                status,
                condition,
                joinedAt,
                leftAt);

        // Assert
        snapshot.Condition
            .Should()
            .Be(condition);

        snapshot.JoinedAt
            .Should()
            .Be(joinedAt);

        snapshot.LeftAt
            .Should()
            .Be(leftAt);
    }
}
