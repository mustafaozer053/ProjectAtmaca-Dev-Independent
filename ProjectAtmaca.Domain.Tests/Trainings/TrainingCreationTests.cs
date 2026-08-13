using FluentAssertions;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Trainings;

public sealed class TrainingCreationTests
{
    [Fact]
    public void Create_Should_CreateTraining_When_DataIsValid()
    {
        // Arrange
        var titleResult =
            TrainingTitle.Create("U15 Sabah Antrenmanı");

        var descriptionResult =
            TrainingDescription.Create(
                "Taktik, kuvvet ve teorik çalışma.");

        var locationResult =
            TrainingLocation.Create("Ana Saha");

        var scheduleResult =
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 3),
                new TimeOnly(16, 0),
                new TimeOnly(18, 0));

        var tacticalDurationResult =
            TrainingTypeDuration.Create(30);

        var strengthDurationResult =
            TrainingTypeDuration.Create(60);

        var theoreticalDurationResult =
            TrainingTypeDuration.Create(30);

        var assignments = new[]
        {
            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                tacticalDurationResult.Value!),

            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                strengthDurationResult.Value!),

            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                theoreticalDurationResult.Value!)
        };

        SeasonOrganization seasonOrganization =
        SeasonOrganization.Create(
        SeasonId.New(),
        OrganizationId.New());

        // Act
        var result = Training.Create(
            seasonOrganization,
            titleResult.Value!,
            descriptionResult.Value!,
            locationResult.Value!,
            scheduleResult.Value!,
            assignments);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();

        Training training = result.Value!;

        training.Title.Should().Be(titleResult.Value);
        training.Description.Should().Be(descriptionResult.Value);
        training.Location.Should().Be(locationResult.Value);
        training.Schedule.Should().Be(scheduleResult.Value);

        training.Schedule.DurationMinutes.Should().Be(120);

        training.TrainingTypeAssignments.Should()
            .HaveCount(3);

        training.TrainingTypeAssignments
            .Sum(assignment => assignment.Duration.Minutes)
            .Should()
            .Be(120);

        training.Status.Should()
            .Be(TrainingStatus.Planned);

        training.TrainingId.Value.Should()
            .NotBe(Guid.Empty);
        training.SeasonOrganization.Should()
            .Be(seasonOrganization);
    }
    [Fact]
    public void Create_Should_Fail_When_AssignmentListIsEmpty()
    {
        // Arrange
        var titleResult =
            TrainingTitle.Create("U15 Sabah Antrenmanı");

        var descriptionResult =
            TrainingDescription.Create(
                "Taktik, kuvvet ve teorik çalışma.");

        var locationResult =
            TrainingLocation.Create("Ana Saha");

        var scheduleResult =
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 3),
                new TimeOnly(16, 0),
                new TimeOnly(18, 0));

        var assignments =
            Array.Empty<TrainingTypeAssignment>();

        SeasonOrganization seasonOrganization =
        SeasonOrganization.Create(
        SeasonId.New(),
        OrganizationId.New());
        // Act
        var result = Training.Create(
            seasonOrganization,
            titleResult.Value!,
            descriptionResult.Value!,
            locationResult.Value!,
            scheduleResult.Value!,
            assignments);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();

        result.Error.Should()
            .BeSameAs(
                TrainingErrors.TrainingTypeAssignmentRequired);
    }
    [Fact]
    public void Create_Should_Fail_When_DuplicateTrainingTypesExist()
    {
        // Arrange
        var titleResult =
            TrainingTitle.Create("U15 Sabah Antrenmanı");

        var descriptionResult =
            TrainingDescription.Create(
                "Taktik çalışması.");

        var locationResult =
            TrainingLocation.Create("Ana Saha");

        var scheduleResult =
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 3),
                new TimeOnly(16, 0),
                new TimeOnly(17, 0));

        var durationResult =
            TrainingTypeDuration.Create(30);

        var trainingTypeId =
            TrainingTypeId.New();

        var assignments = new[]
        {
        TrainingTypeAssignment.Create(
            trainingTypeId,
            durationResult.Value!),

        TrainingTypeAssignment.Create(
            trainingTypeId,
            durationResult.Value!)
    };

        SeasonOrganization seasonOrganization =
        SeasonOrganization.Create(
        SeasonId.New(),
        OrganizationId.New());
        // Act
        var result = Training.Create(
            seasonOrganization,
            titleResult.Value!,
            descriptionResult.Value!,
            locationResult.Value!,
            scheduleResult.Value!,
            assignments);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Value.Should().BeNull();

        result.Error.Should()
            .BeSameAs(
                TrainingErrors.DuplicateTrainingTypeAssignment);
    }
    [Fact]
    public void Create_Should_Fail_When_TotalTrainingTypeDurationDoesNotMatchSchedule()
    {
        // Arrange
        var titleResult =
            TrainingTitle.Create("U15 Sabah Antrenmanı");

        var descriptionResult =
            TrainingDescription.Create(
                "Taktik, kuvvet ve teorik çalışma.");

        var locationResult =
            TrainingLocation.Create("Ana Saha");

        var scheduleResult =
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 3),
                new TimeOnly(16, 0),
                new TimeOnly(18, 0));

        var assignments = new[]
        {
        TrainingTypeAssignment.Create(
            TrainingTypeId.New(),
            TrainingTypeDuration.Create(30).Value!),

        TrainingTypeAssignment.Create(
            TrainingTypeId.New(),
            TrainingTypeDuration.Create(30).Value!),

        TrainingTypeAssignment.Create(
            TrainingTypeId.New(),
            TrainingTypeDuration.Create(30).Value!)
    };

        SeasonOrganization seasonOrganization =
        SeasonOrganization.Create(
        SeasonId.New(),
        OrganizationId.New());
        // Act
        var result = Training.Create(
            seasonOrganization,
            titleResult.Value!,
            descriptionResult.Value!,
            locationResult.Value!,
            scheduleResult.Value!,
            assignments);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Value.Should().BeNull();

        result.Error.Should()
            .BeSameAs(
                TrainingErrors.TrainingTypeDurationMismatch);
    }
}
