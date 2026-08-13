using FluentAssertions;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.Trainings.DomainEvents;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Trainings;

public sealed class TrainingDomainEventTests
{
    [Fact]
    public void Create_Should_RaiseTrainingPlannedDomainEvent()
    {
        // Arrange
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
        // Act
        var result = Training.Create(
            seasonOrganization,
            title,
            description,
            location,
            schedule,
            assignments);

        // Assert
        result.IsSuccess.Should().BeTrue();

        Training training = result.Value!;

        training.DomainEvents
            .Should()
            .ContainSingle();

        training.DomainEvents
            .Single()
            .Should()
            .BeOfType<TrainingPlannedDomainEvent>();
    }
    [Fact]
    public void Confirm_Should_RaiseTrainingConfirmedDomainEvent_OnlyOnce()
    {
        // Arrange
        Training training = CreateTraining();

        training.DomainEvents.Should().HaveCount(1);

        // Act
        training.Confirm();

        training.Confirm();

        // Assert
        training.DomainEvents
            .OfType<TrainingConfirmedDomainEvent>()
            .Should()
            .HaveCount(1);

        training.DomainEvents.Should()
            .HaveCount(2);
    }
    private static Training CreateTraining()
    {
        TrainingTitle title =
            TrainingTitle.Create(
                "U15 Sabah Antrenmanı").Value!;

        TrainingDescription description =
            TrainingDescription.Create(
                "Taktik, kuvvet ve teorik çalışma.").Value!;

        TrainingLocation location =
            TrainingLocation.Create(
                "Ana Saha").Value!;

        TrainingSchedule schedule =
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
}
