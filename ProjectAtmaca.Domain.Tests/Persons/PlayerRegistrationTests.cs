using FluentAssertions;
using ProjectAtmaca.Domain.Players;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PlayerRegistrationTests
{
    [Fact]
    public void Create_Should_CreateActiveRegistration()
    {
        var startDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var registeredAt = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        var result = PlayerRegistration.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            startDate,
            registeredAt,
            "İlk kulüp kaydı");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(PlayerRegistrationStatus.Active);
        result.Value.StartDateUtc.Should().Be(startDate);
        result.Value.RegisteredAtUtc.Should().Be(registeredAt);
        result.Value.Notes.Should().Be("İlk kulüp kaydı");
    }

    [Fact]
    public void Create_Should_RejectRegistrationDateBeforeStartDate()
    {
        var result = PlayerRegistration.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            startDateUtc: new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            registeredAtUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PLAYER_REGISTRATION_DATE_ORDER_INVALID");
    }

    [Fact]
    public void Close_Should_PreserveEndDateAndStatus()
    {
        var startDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var registration = PlayerRegistration.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            startDateUtc: startDate,
            registeredAtUtc: startDate).Value!;

        var result = registration.Close(
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PlayerRegistrationStatus.Completed);

        result.IsSuccess.Should().BeTrue();
        registration.Status.Should().Be(PlayerRegistrationStatus.Completed);
        registration.EndDateUtc.Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
