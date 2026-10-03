using FluentAssertions;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Domain.Tests.AtmacaCards;

public sealed class AtmacaCardIssueTests
{
    [Fact]
    public void Issue_Should_PreserveExplicitUtcIssuanceTime()
    {
        var issuedAtUtc = new DateTime(2020, 3, 4, 12, 30, 0, DateTimeKind.Utc).AddTicks(1234567);

        var result = AtmacaCard.Issue(Guid.NewGuid(), CreateCardNumber(), issuedAtUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IssuedAtUtc.Ticks.Should().Be(issuedAtUtc.Ticks);
        result.Value.IssuedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Issue_Should_DefaultCardToActive_AndAllowStatusChanges()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;

        card.IsActive.Should().BeTrue();
        card.Deactivate();
        card.IsActive.Should().BeFalse();
        card.Activate();
        card.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpsertSportsProfile_ShouldKeepOneProfilePerSport_AndPreserveItsId()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;

        var first = card.UpsertSportsProfile(
            "Futbol",
            "LIC-001",
            new DateOnly(2018, 7, 1),
            new DateOnly(2020, 8, 15),
            AthleteCompetitionLevel.Amateur,
            false).Value!;
        var update = card.UpsertSportsProfile(
            "futbol",
            "LIC-002",
            new DateOnly(2019, 1, 1),
            new DateOnly(2021, 8, 15),
            AthleteCompetitionLevel.Professional,
            true).Value!;

        update.Id.Should().Be(first.Id);
        update.SportName.Should().Be("futbol");
        update.LicenseNumber!.Value.Should().Be("LIC-002");
        card.SportsProfiles.Should().ContainSingle();

        card.UpsertSportsProfile("Basketbol", null, null, null, null, null)
            .IsSuccess.Should().BeTrue();
        card.SportsProfiles.Should().HaveCount(2);
    }

    [Fact]
    public void UpsertSportsProfile_ShouldRejectInvalidSportName()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;

        var result = card.UpsertSportsProfile(" ", null, null, null, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ATMACA_CARD_SPORTS_PROFILE_SPORT_INVALID");
    }

    [Fact]
    public void UpsertSportsProfile_ShouldAssignMultiplePositionsFromMatchingSport()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;
        var goalkeeper = Position.Create("Futbol", "GK", "Kaleci").Value!;
        var defender = Position.Create("Futbol", "DF", "Defans").Value!;

        var result = card.UpsertSportsProfile(
            "Futbol", null, null, null, null, null, [goalkeeper, defender]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Positions.Select(position => position.Id)
            .Should().BeEquivalentTo([goalkeeper.Id, defender.Id]);
    }

    [Fact]
    public void UpsertSportsProfile_ShouldRejectPositionFromDifferentSport()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;
        var position = Position.Create("Basketbol", "PG", "Oyun kurucu").Value!;

        var result = card.UpsertSportsProfile(
            "Futbol", null, null, null, null, null, [position]);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ATMACA_CARD_SPORTS_PROFILE_POSITION_SPORT_MISMATCH");
        card.SportsProfiles.Should().BeEmpty();
    }

    [Fact]
    public void AddMeasurement_ShouldPreserveDatedHistory()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;

        var earlier = card.AddMeasurement(
            new DateOnly(2026, 1, 1), 150.25m, 45.5m);
        var later = card.AddMeasurement(
            new DateOnly(2026, 4, 1), 155m, null);

        earlier.IsSuccess.Should().BeTrue();
        later.IsSuccess.Should().BeTrue();
        card.Measurements.Should().HaveCount(2);
        card.Measurements.Single(x => x.MeasuredOn == new DateOnly(2026, 1, 1))
            .WeightKilograms.Should().Be(45.5m);
        card.Measurements.Single(x => x.MeasuredOn == new DateOnly(2026, 4, 1))
            .HeightCentimeters.Should().Be(155m);
    }

    [Fact]
    public void UpdateEducation_ShouldStoreSchoolDetailsWhenCurrentlyStudying()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;

        var result = card.UpdateEducation(true, "  Atatürk Lisesi ", " 10. sınıf ", " 1234 ");

        result.IsSuccess.Should().BeTrue();
        card.IsCurrentlyStudying.Should().BeTrue();
        card.SchoolName.Should().Be("Atatürk Lisesi");
        card.SchoolGrade.Should().Be("10. sınıf");
        card.SchoolNumber.Should().Be("1234");
    }

    [Fact]
    public void UpdateEducation_ShouldClearSchoolDetailsWhenNotCurrentlyStudying()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;
        card.UpdateEducation(true, "Atatürk Lisesi", "10. sınıf", "1234");

        var result = card.UpdateEducation(false, null, null, null);

        result.IsSuccess.Should().BeTrue();
        card.IsCurrentlyStudying.Should().BeFalse();
        card.SchoolName.Should().BeNull();
        card.SchoolGrade.Should().BeNull();
        card.SchoolNumber.Should().BeNull();
    }

    [Fact]
    public void UpdateEducation_ShouldRejectOverlongSchoolNameWithoutChangingExistingData()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;
        card.UpdateEducation(true, "Atatürk Lisesi", "10. sınıf", "1234");

        var result = card.UpdateEducation(true, new string('A', 151), null, null);

        result.IsFailure.Should().BeTrue();
        card.SchoolName.Should().Be("Atatürk Lisesi");
    }

    [Fact]
    public void AddMeasurement_ShouldRejectEmptyValuesAndDuplicateDates()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            ValidIssuedAtUtc).Value!;
        var date = new DateOnly(2026, 1, 1);

        var empty = card.AddMeasurement(date, null, null);
        var first = card.AddMeasurement(date, 150m, 45m);
        var duplicate = card.AddMeasurement(date, 151m, 46m);

        empty.Error!.Code.Should().Be("ATMACA_CARD_MEASUREMENT_VALUE_REQUIRED");
        first.IsSuccess.Should().BeTrue();
        duplicate.Error!.Code.Should().Be("ATMACA_CARD_MEASUREMENT_DATE_DUPLICATE");
        card.Measurements.Should().ContainSingle();
    }

    [Fact]
    public void Issue_Should_RejectEmptyPersonId()
    {
        var result = AtmacaCard.Issue(
            Guid.Empty,
            CreateCardNumber(),
            ValidIssuedAtUtc);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be("ATMACA_CARD_PERSON_REQUIRED");
    }

    [Fact]
    public void Issue_Should_RejectMissingCardNumber()
    {
        var result = AtmacaCard.Issue(Guid.NewGuid(), null!, ValidIssuedAtUtc);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be("ATMACA_CARD_NUMBER_REQUIRED");
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void Issue_Should_RejectMissingIssuanceTime(DateTimeKind kind)
    {
        var result = AtmacaCard.Issue(
            Guid.NewGuid(),
            CreateCardNumber(),
            DateTime.SpecifyKind(default, kind));

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be("ATMACA_CARD_ISSUED_AT_REQUIRED");
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void Issue_Should_RejectNonUtcIssuanceTime(DateTimeKind kind)
    {
        var result = AtmacaCard.Issue(
            Guid.NewGuid(), CreateCardNumber(), DateTime.SpecifyKind(ValidIssuedAtUtc, kind));

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be("ATMACA_CARD_ISSUED_AT_UTC_REQUIRED");
    }

    [Fact]
    public void Issue_Should_PreservePersonAndNumber_WithoutInventingAnActor()
    {
        var personId = Guid.NewGuid();
        var cardNumber = CreateCardNumber();

        var result = AtmacaCard.Issue(personId, cardNumber, ValidIssuedAtUtc);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        var card = result.Value!;
        card.PersonId.Should().Be(personId);
        card.CardNumber.Should().Be(cardNumber);
        card.CreatedByActorId.Should().BeNull();
        card.LastModifiedByActorId.Should().BeNull();
        card.IssuedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        card.IssuedAtUtc.Should().NotBe(default(DateTime));
    }

    [Fact]
    public void Issue_Should_UseCanonicalAudit_WithoutChangingIssuanceTime()
    {
        var card = AtmacaCard.Issue(Guid.NewGuid(), CreateCardNumber(), ValidIssuedAtUtc).Value!;
        var creator = ActorId.New();
        var modifier = ActorId.New();

        card.SetCreatedBy(creator);
        card.MarkAsModified(modifier);

        card.CreatedByActorId.Should().Be(creator);
        card.LastModifiedByActorId.Should().Be(modifier);
        card.IssuedAtUtc.Ticks.Should().Be(ValidIssuedAtUtc.Ticks);
        card.IssuedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        typeof(AtmacaCard).GetProperty("IssuedBy").Should().BeNull();
    }

    private static DateTime ValidIssuedAtUtc =>
        new(2020, 3, 4, 12, 30, 0, DateTimeKind.Utc);

    private static AtmacaCardNumber CreateCardNumber() =>
        AtmacaCardNumber.Create("ATM-000001").Value!;
}
