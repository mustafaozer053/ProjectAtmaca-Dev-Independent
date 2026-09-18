using FluentAssertions;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Scouting;
using ProjectAtmaca.Domain.Scouting.ValueObjects;

namespace ProjectAtmaca.Domain.Tests.Scouting;

public sealed class ScoutingConversionTests
{
    [Fact]
    public void ConvertToPlayer_Should_CreateConversionAndSetPositiveDecision()
    {
        var candidate = ScoutingCandidate.Create(
            PersonName.Create("Deniz Aras").Value!,
            null,
            null,
            BirthDate.Create(new DateTime(2014, 8, 10)),
            null,
            Country.Create("TR", "Türkiye"),
            null,
            null,
            null,
            null,
            InitialScoutingSource.Create(InitialScoutingSourceType.Training).Value!,
            new DateOnly(2026, 3, 15),
            ObservationType.Training,
            "Kamp gözlemi",
            "Rize Spor",
            "U12 A",
            null,
            null,
            null,
            "Hızlı ve güçlü",
            "Dengesiz defans",
            ObserverRecommendation.Positive,
            "Geliştirilebilir",
            PersonName.Create("Mert Aydın").Value!,
            Guid.NewGuid()).Value!;

        var personId = Guid.NewGuid();
        var clubId = Guid.NewGuid();
        var academyId = Guid.NewGuid();

        var result = candidate.ConvertToPlayer(
            personId,
            clubId,
            academyId,
            teamId: Guid.NewGuid(),
            convertedAtUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            notes: "Kulübe katılım kararı");

        result.IsSuccess.Should().BeTrue();
        result.Value!.PersonId.Should().Be(personId);
        result.Value.ClubId.Should().Be(clubId);
        result.Value.AcademyId.Should().Be(academyId);
        result.Value.Notes.Should().Be("Kulübe katılım kararı");
        candidate.ScoutingDecision.Should().Be(ScoutingDecision.Positive);
    }

    [Fact]
    public void ConvertToPlayer_Should_RejectEmptyAcademyId()
    {
        var candidate = ScoutingCandidate.Create(
            PersonName.Create("Aylin Kırık").Value!,
            null,
            null,
            BirthDate.Create(new DateTime(2013, 9, 3)),
            null,
            Country.Create("TR", "Türkiye"),
            null,
            null,
            null,
            null,
            InitialScoutingSource.Create(InitialScoutingSourceType.Reference, PersonName.Create("Ahmet Usta").Value!).Value!,
            new DateOnly(2026, 2, 10),
            ObservationType.Training,
            "Gözlem",
            "Futbol Kulübü",
            "U13 A",
            null,
            null,
            null,
            "İyi koordinasyon",
            null,
            ObserverRecommendation.ContinueWatching,
            null,
            PersonName.Create("Esra Demir").Value!,
            Guid.NewGuid()).Value!;

        var result = candidate.ConvertToPlayer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            notes: "Geçersiz akademi");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("SCOUTING_CONVERSION_ACADEMY_ID_REQUIRED");
    }
}
