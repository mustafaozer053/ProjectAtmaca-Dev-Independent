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

    private static SeasonTeam CreateTeam()
    {
        return SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "U16").Value!;
    }
}
