using FluentAssertions;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Trainings;

public sealed class TrainingBehaviorTests
{
    [Fact]
    public void Rename_Should_ChangeTitle_When_NewTitleIsValid()
    {
        // Arrange
        var training = CreateTraining();

        var newTitle =
            TrainingTitle.Create(
                "U15 Akşam Antrenmanı").Value!;

        // Act
        training.Rename(newTitle);

        // Assert
        training.Title.Should().Be(newTitle);
    }

    private static Training CreateTraining()
    {
        var title =
            TrainingTitle.Create(
                "U15 Sabah Antrenmanı").Value!;

        var description =
            TrainingDescription.Create(
                "Taktik, kuvvet ve teorik çalışma.").Value!;

        var location =
            TrainingLocation.Create(
                "Ana Saha").Value!;

        var schedule =
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 3),
                new TimeOnly(16, 0),
                new TimeOnly(18, 0)).Value!;

        var assignments = new[]
        {
            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                TrainingTypeDuration.Create(30).Value!),

            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                TrainingTypeDuration.Create(60).Value!),

            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                TrainingTypeDuration.Create(30).Value!)
        };
        SeasonOrganization seasonOrganization =
        SeasonOrganization.Create(
        SeasonId.New(),
        OrganizationId.New());
        return Training.Create(
            seasonOrganization,
            title,
            description,
            location,
            schedule,
            assignments).Value!;
    }
    [Fact]
    public void Reschedule_Should_ChangeSchedule_When_NewScheduleIsValid()
    {
        // Arrange
        Training training = CreateTraining();

        TrainingSchedule newSchedule =
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 4),
                new TimeOnly(17, 0),
                new TimeOnly(19, 0)).Value!;

        var currentAssignments =
            training.TrainingTypeAssignments.ToArray();

        // Act
        var result = training.Reschedule(
            newSchedule,
            currentAssignments);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        training.Schedule.Should().Be(newSchedule);
        training.Schedule.DurationMinutes.Should().Be(120);

        training.TrainingTypeAssignments.Should()
            .HaveCount(3);

        training.TrainingTypeAssignments
            .Sum(assignment => assignment.Duration.Minutes)
            .Should()
            .Be(120);
    }
    [Fact]
    public void Reschedule_Should_Fail_When_AssignmentDurationsDoNotMatchNewSchedule()
    {
        // Arrange
        Training training = CreateTraining();

        TrainingSchedule originalSchedule =
            training.Schedule;

        var originalAssignments =
            training.TrainingTypeAssignments.ToArray();

        TrainingSchedule shorterSchedule =
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 4),
                new TimeOnly(17, 0),
                new TimeOnly(18, 30)).Value!;

        // Mevcut dağılım hâlâ 120 dakika:
        // 30 + 60 + 30
        // Yeni Schedule ise 90 dakika.
        var incompatibleAssignments =
            training.TrainingTypeAssignments.ToArray();

        // Act
        var result = training.Reschedule(
            shorterSchedule,
            incompatibleAssignments);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                TrainingErrors.TrainingTypeDurationMismatch);

        training.Schedule.Should()
            .Be(originalSchedule);

        training.TrainingTypeAssignments.Should()
            .HaveCount(originalAssignments.Length);

        training.TrainingTypeAssignments.Should()
            .ContainInOrder(originalAssignments);
    }
    [Fact]
    public void UpdateTrainingTypeAssignments_Should_UpdateAssignments_When_DurationsMatchSchedule()
    {
        // Arrange
        Training training = CreateTraining();

        var updatedAssignments = new[]
        {
        TrainingTypeAssignment.Create(
            TrainingTypeId.New(),
            TrainingTypeDuration.Create(40).Value!),

        TrainingTypeAssignment.Create(
            TrainingTypeId.New(),
            TrainingTypeDuration.Create(50).Value!),

        TrainingTypeAssignment.Create(
            TrainingTypeId.New(),
            TrainingTypeDuration.Create(30).Value!)
    };

        // Act
        var result =
            training.UpdateTrainingTypeAssignments(
                updatedAssignments);

        // Assert
        result.IsSuccess.Should().BeTrue();

        training.TrainingTypeAssignments
            .Should()
            .HaveCount(3);

        training.TrainingTypeAssignments
            .Sum(x => x.Duration.Minutes)
            .Should()
            .Be(120);

        training.TrainingTypeAssignments
            .Select(x => x.Duration.Minutes)
            .Should()
            .ContainInOrder(40, 50, 30);
    }
    [Fact]
    public void Confirm_Should_ChangeStatus_When_TrainingIsPlanned()
    {
        // Arrange
        Training training = CreateTraining();

        training.Status.Should()
            .Be(TrainingStatus.Planned);

        // Act
        training.Confirm();

        // Assert
        training.Status.Should()
            .Be(TrainingStatus.Confirmed);
    }

    [Fact]
    public void Cancel_Should_ChangeStatus_When_TrainingIsPlanned()
    {
        Training training = CreateTraining();

        var result = training.Cancel();

        result.IsSuccess.Should().BeTrue();
        training.Status.Should().Be(TrainingStatus.Cancelled);
    }

    [Fact]
    public void Confirm_Should_Not_Reactivate_CancelledTraining()
    {
        Training training = CreateTraining();

        training.Cancel();
        training.Confirm();

        training.Status.Should().Be(TrainingStatus.Cancelled);
    }

    [Fact]
    public void Reschedule_Should_Reject_CancelledTraining()
    {
        Training training = CreateTraining();
        training.Cancel();

        var result = training.Reschedule(
            TrainingSchedule.Create(
                new DateOnly(2026, 8, 4),
                new TimeOnly(17, 0),
                new TimeOnly(18, 0)).Value!,
            training.TrainingTypeAssignments.ToArray());

        result.Error.Should().Be(TrainingErrors.RescheduleCancelledTraining);
        training.Status.Should().Be(TrainingStatus.Cancelled);
    }
}
