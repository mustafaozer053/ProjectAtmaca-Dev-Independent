using FluentAssertions;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.SeasonTeams;

public sealed class SeasonTeamTests
{
    [Fact]
    public void Create_Should_CreateActiveSeasonTeam()
    {
        var result = SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "ÇAYKUR RİZESPOR U16 TAKIMI");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("ÇAYKUR RİZESPOR U16 TAKIMI");
        result.Value.Status.Should().Be(SeasonTeamStatus.Active);
        result.Value.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_Should_MarkSeasonTeamInactive_WithoutChangingMembershipHistory()
    {
        var team = CreateTeam();
        var cardId = AtmacaCardId.New();
        var period = AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!;

        team.AddMembership(cardId, period).IsSuccess.Should().BeTrue();
        team.Deactivate();

        team.Status.Should().Be(SeasonTeamStatus.Inactive);
        team.Memberships.Should().ContainSingle();
        team.HasActiveMembership(cardId, new DateTime(2026, 8, 1))
            .Should().BeTrue();
    }

    [Fact]
    public void Activate_Should_RestoreActiveStatus()
    {
        var team = CreateTeam();

        team.Deactivate();
        team.Activate();

        team.Status.Should().Be(SeasonTeamStatus.Active);
    }

    [Fact]
    public void AddMembership_Should_AllowSameCardInDifferentSeasonTeams()
    {
        var firstTeam = CreateTeam();
        var secondTeam = CreateTeam();
        var cardId = AtmacaCardId.New();
        var period = AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!;

        var firstResult = firstTeam.AddMembership(cardId, period);
        var secondResult = secondTeam.AddMembership(cardId, period);

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddMembership_Should_RejectDuplicateCardInSameSeasonTeam()
    {
        var team = CreateTeam();
        var cardId = AtmacaCardId.New();
        var period = AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!;

        team.AddMembership(cardId, period).IsSuccess.Should().BeTrue();
        var result = team.AddMembership(cardId, period);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(SeasonTeamErrors.DuplicateMembership);
    }

    [Fact]
    public void AddMembership_Should_AllowNonOverlappingMembershipPeriods()
    {
        var team = CreateTeam();
        var cardId = AtmacaCardId.New();

        var firstPeriod = AssignmentPeriod.Create(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 31)).Value!;
        var secondPeriod = AssignmentPeriod.Create(
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 31)).Value!;

        team.AddMembership(cardId, firstPeriod).IsSuccess.Should().BeTrue();
        var result = team.AddMembership(cardId, secondPeriod);

        result.IsSuccess.Should().BeTrue();
        team.Memberships.Should().HaveCount(2);
    }

    [Fact]
    public void HasActiveMembership_Should_UseMembershipPeriod()
    {
        var team = CreateTeam();
        var cardId = AtmacaCardId.New();
        var period = AssignmentPeriod.Create(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 31)).Value!;

        team.AddMembership(cardId, period);

        team.HasActiveMembership(cardId, new DateTime(2026, 7, 1))
            .Should().BeTrue();
        team.HasActiveMembership(cardId, new DateTime(2026, 8, 1))
            .Should().BeFalse();
    }

    [Fact]
    public void EndMembership_Should_PreserveMembershipAndSetEndDate()
    {
        var team = CreateTeam();
        var cardId = AtmacaCardId.New();
        var period = AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!;
        var membership = team.AddMembership(cardId, period).Value!;

        var result = team.EndMembership(
            membership.SeasonTeamMembershipId,
            new DateTime(2026, 7, 31));

        result.IsSuccess.Should().BeTrue();
        team.Memberships.Should().ContainSingle()
            .Which.Period.EndDate.Should().Be(new DateTime(2026, 7, 31));
        team.HasActiveMembership(cardId, new DateTime(2026, 8, 1))
            .Should().BeFalse();
    }

    [Fact]
    public void AddMembershipAssignment_Should_AllowMultipleKindsAtSameTime()
    {
        var team = CreateTeam();
        var membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!).Value!;

        var playerResult = team.AddMembershipAssignment(
            membership.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Role,
            Guid.NewGuid(),
            "Player",
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!);

        var captainResult = team.AddMembershipAssignment(
            membership.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Role,
            Guid.NewGuid(),
            "Captain",
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!);

        playerResult.IsSuccess.Should().BeTrue();
        captainResult.IsSuccess.Should().BeTrue();
        membership.Assignments.Should().HaveCount(2);
    }

    [Fact]
    public void AddMembershipAssignment_Should_RejectOverlappingDuplicateDefinition()
    {
        var team = CreateTeam();
        var membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!).Value!;
        Guid definitionId = Guid.NewGuid();

        team.AddMembershipAssignment(
            membership.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Role,
            definitionId,
            "Player",
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!)
            .IsSuccess.Should().BeTrue();

        var duplicate = team.AddMembershipAssignment(
            membership.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Role,
            definitionId,
            "Player",
            AssignmentPeriod.Create(new DateTime(2026, 8, 1)).Value!);

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Should().BeSameAs(SeasonTeamErrors.DuplicateAssignment);
    }

    [Fact]
    public void EndMembershipAssignment_Should_SetAssignmentEndDate()
    {
        var team = CreateTeam();
        var membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!).Value!;

        var assignment = team.AddMembershipAssignment(
            membership.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Duty,
            Guid.NewGuid(),
            "Takım Teknik Sorumlusu",
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!)
            .Value!;

        var result = team.EndMembershipAssignment(
            membership.SeasonTeamMembershipId,
            assignment.SeasonTeamMembershipAssignmentId,
            new DateTime(2026, 7, 31));

        result.IsSuccess.Should().BeTrue();
        membership.Assignments.Should().ContainSingle()
            .Which.Period.EndDate.Should().Be(new DateTime(2026, 7, 31));
    }

    private static SeasonTeam CreateTeam()
    {
        return SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "U16").Value!;
    }
}
