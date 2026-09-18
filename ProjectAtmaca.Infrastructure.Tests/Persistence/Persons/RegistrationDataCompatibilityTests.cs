using System.Text.Json;
using FluentAssertions;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons.Registration;
using ProjectAtmaca.Infrastructure.Persistence.Persons;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Persons;

public sealed class RegistrationDataCompatibilityTests
{
    [Fact]
    public void Snapshot_Should_PreserveExplicitConfirmationAndReason()
    {
        var input = new PersonRegistrationInput(PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(2010, 1, 1)), Country.Create("TR", "Türkiye"),
            TurkishCitizenshipStatus.NotTurkishCitizen, PassportNumber: "AB12345")
        { ConfirmPossiblePassportDuplicate = true, PassportDuplicateReason = "Different person confirmed.",
            Citizenships = [new(Country.Create("FR", "France")), new(Country.Create("DE", "Germany"), new DateOnly(2020, 1, 1))] };
        var json = JsonSerializer.Serialize(RegistrationData.From(input));
        var restored = JsonSerializer.Deserialize<RegistrationData>(json)!.ToDomain();
        new CompletedPersonRegistration(input, new RegistrationReceipt(Guid.NewGuid(), Guid.NewGuid(), "ATM-000001", DateTime.UtcNow))
            .Matches(restored).Should().BeTrue();
    }

    [Fact]
    public void HistoricalSnapshotWithoutConfirmation_Should_NotAcquireImplicitConsent()
    {
        const string json = """
            {"Version":1,"Name":"Test Person","BirthDate":"2010-01-01T00:00:00",
             "BirthCountry":{"Code":"FR","Name":"France"},
             "Identity":{"Status":3,"NationalId":null,"Passport":"AB12345","AcquiredOn":null},
             "BloodType":0}
            """;
        var restored = JsonSerializer.Deserialize<RegistrationData>(json)!.ToDomain();
        restored.ConfirmPossiblePassportDuplicate.Should().BeFalse();
        restored.PassportDuplicateReason.Should().BeNull();
        restored.Citizenships.Should().BeEmpty();
    }
}
