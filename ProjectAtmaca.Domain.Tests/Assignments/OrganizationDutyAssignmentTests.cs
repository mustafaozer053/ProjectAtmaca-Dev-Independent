using FluentAssertions;
using ProjectAtmaca.Domain.Assignments;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Tests.Assignments;

public sealed class OrganizationDutyAssignmentTests
{
    [Fact]
    public void Create_ShouldAllowSeasonIndependentOrganizationDuty()
    {
        var title = AssignmentTitle.Create("Başkan").Value!;

        var result = OrganizationDutyAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            new DateTime(2024, 6, 1),
            null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EndDate.Should().BeNull();
    }

    [Fact]
    public void End_ShouldPreserveTheDutyAndValidateItsDate()
    {
        var assignment = CreateAssignment(new DateTime(2024, 6, 1));

        var beforeStart = assignment.End(new DateTime(2024, 5, 31));
        var ended = assignment.End(new DateTime(2025, 8, 10));
        var endedAgain = assignment.End(new DateTime(2025, 8, 10));

        beforeStart.Error!.Code.Should().Be("ORGANIZATION_DUTY_PERIOD_INVALID");
        ended.IsSuccess.Should().BeTrue();
        endedAgain.Error!.Code.Should().Be("ORGANIZATION_DUTY_ALREADY_ENDED");
        assignment.EndDate.Should().Be(new DateTime(2025, 8, 10));
    }

    [Fact]
    public void Create_ShouldKeepAthleteAsClassificationRatherThanOrganizationDuty()
    {
        var result = OrganizationDutyAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AssignmentTitle.Create("Sporcu").Value!,
            new DateTime(2024, 6, 1),
            null);

        result.Error!.Code.Should().Be("ORGANIZATION_DUTY_CLASSIFICATION_REQUIRED");
    }

    private static OrganizationDutyAssignment CreateAssignment(DateTime startDate) =>
        OrganizationDutyAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AssignmentTitle.Create("Başkan").Value!,
            startDate,
            null).Value!;
}
