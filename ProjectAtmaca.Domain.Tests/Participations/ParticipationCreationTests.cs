using FluentAssertions;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Participations;

public sealed class ParticipationCreationTests
{
    [Fact]
    public void Create_Should_Succeed_With_NotRecorded_InitialState()
    {
        // Arrange
        ActivityReference activityReference =
            CreateActivityReference();

        AtmacaCardId atmacaCardId =
            CreateAtmacaCardId();

        // Act
        Result<Participation> result =
            Participation.Create(
                activityReference,
                atmacaCardId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        result.Value.Should().NotBeNull();

        Participation participation =
            result.Value!;

        participation.Status.Should()
            .Be(ParticipationStatus.NotRecorded);

        participation.Condition.Should()
            .BeNull();

        participation.JoinedAt.Should()
            .BeNull();

        participation.LeftAt.Should()
            .BeNull();

        participation.Note.Should()
            .BeNull();

        participation.ActivityReference.Should()
            .Be(activityReference);

        participation.AtmacaCardId.Should()
            .Be(atmacaCardId);
    }

    [Fact]
    public void Create_Should_Not_Fabricate_AttendanceOutcome()
    {
        // Arrange
        ActivityReference activityReference =
            CreateActivityReference();

        AtmacaCardId atmacaCardId =
            CreateAtmacaCardId();

        // Act
        Participation participation =
            Participation.Create(
                activityReference,
                atmacaCardId).Value!;

        // Assert
        participation.Status.Should()
            .NotBe(ParticipationStatus.Present);

        participation.Status.Should()
            .NotBe(ParticipationStatus.Absent);

        participation.Status.Should()
            .Be(ParticipationStatus.NotRecorded);
    }

    [Fact]
    public void Create_Should_Fail_When_ActivityReference_Is_Null()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            CreateAtmacaCardId();

        // Act
        Result<Participation> result =
            Participation.Create(
                null!,
                atmacaCardId);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Value.Should().BeNull();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors.ActivityReferenceRequired);
    }

    [Fact]
    public void Create_Should_Fail_When_AtmacaCardId_Is_Empty()
    {
        // Arrange
        ActivityReference activityReference =
            CreateActivityReference();

        AtmacaCardId atmacaCardId =
            default;

        // Act
        Result<Participation> result =
            Participation.Create(
                activityReference,
                atmacaCardId);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Value.Should().BeNull();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors.AtmacaCardRequired);
    }

    private static ActivityReference CreateActivityReference()
    {
        TrainingId trainingId =
            TrainingId.New();

        return ActivityReference.ForTraining(
            trainingId);
    }

    private static AtmacaCardId CreateAtmacaCardId()
    {
        return AtmacaCardId.New();
    }
}