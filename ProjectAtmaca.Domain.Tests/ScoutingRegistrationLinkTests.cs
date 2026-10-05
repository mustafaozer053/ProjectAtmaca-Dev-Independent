using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Scouting;
using ProjectAtmaca.Domain.Scouting.ValueObjects;

namespace ProjectAtmaca.Domain.Tests.Scouting;

public sealed class ScoutingRegistrationLinkTests
{
    private static ScoutingCandidate Create() => ScoutingCandidate.Create(
        PersonName.Create("Ali Veli").Value!, null, null, BirthDate.Create(new DateTime(2012, 1, 1)), null, null,
        null, null, null, null,
        InitialScoutingSource.Create(InitialScoutingSourceType.Match).Value!,
        DateOnly.FromDateTime(DateTime.Today), ObservationType.Match, "Turnuva", "Kulüp", "U13", null, null, null,
        null, null, null, null, PersonName.Create("Gözlemci Kişi").Value!, Guid.NewGuid(), null).Value!;

    [Fact]
    public void LinkRegisteredPerson_Should_StoreCardDetails()
    {
        var candidate = Create();
        var personId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        Assert.True(candidate.LinkRegisteredPerson(personId, cardId, " AC-1 ").IsSuccess);
        Assert.Equal(personId, candidate.RegisteredPersonId);
        Assert.Equal(cardId, candidate.RegisteredAtmacaCardId);
        Assert.Equal("AC-1", candidate.RegisteredCardNumber);
    }

    [Fact]
    public void LinkRegisteredPerson_Should_RejectDifferentPersonOnceLinked()
    {
        var candidate = Create();
        candidate.LinkRegisteredPerson(Guid.NewGuid(), Guid.NewGuid(), "AC-1");

        Assert.True(candidate.LinkRegisteredPerson(Guid.NewGuid(), Guid.NewGuid(), "AC-2").IsFailure);
    }

    [Fact]
    public void LinkRegisteredPerson_Should_RejectEmptyValues() =>
        Assert.True(Create().LinkRegisteredPerson(Guid.Empty, Guid.NewGuid(), "AC-1").IsFailure);
}