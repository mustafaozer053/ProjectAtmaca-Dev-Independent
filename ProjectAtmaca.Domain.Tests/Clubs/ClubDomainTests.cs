using FluentAssertions;
using ProjectAtmaca.Domain.Clubs;

namespace ProjectAtmaca.Domain.Tests.Clubs;

public sealed class ClubDomainTests
{
    [Fact]
    public void Create_Should_CreateActiveClub()
    {
        var result = Club.Create("Rize Spor", "RSP", "Kulüp tanımı");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Rize Spor");
        result.Value.ShortName.Should().Be("RSP");
        result.Value.Description.Should().Be("Kulüp tanımı");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_RejectEmptyName()
    {
        var result = Club.Create("   ");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CLUB_NAME_REQUIRED");
    }

    [Fact]
    public void Academy_Create_Should_RequireClubId()
    {
        var result = Academy.Create(Guid.Empty, "U12 Akademisi");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ACADEMY_CLUB_ID_REQUIRED");
    }

    [Fact]
    public void Team_Create_Should_LinkAcademyAndClub()
    {
        var clubId = Guid.NewGuid();
        var academyId = Guid.NewGuid();

        var result = Team.Create(clubId, academyId, "U12 A Takımı", "U12", "Yalın");

        result.IsSuccess.Should().BeTrue();
        result.Value!.ClubId.Should().Be(clubId);
        result.Value.AcademyId.Should().Be(academyId);
        result.Value.AgeGroup.Should().Be("U12");
        result.Value.Category.Should().Be("Yalın");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Branch_Create_Should_KeepNameTrimmed()
    {
        var result = Branch.Create("  Futbol  ", "Açıklama");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Futbol");
    }
}
