using FluentAssertions;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PersonIdentitySeparationTests
{
    [Fact]
    public void Create_Should_KeepBirthCountry_WithoutSingleIdentityOrCitizenshipState()
    {
        var country = Country.Create("FR", "France");

        var result = Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(2005, 1, 2)),
            country);

        result.IsSuccess.Should().BeTrue();
        var person = result.Value!;
        person.BirthCountry.Should().Be(country);
        person.BirthPlace.Should().BeNull();
        typeof(Person).GetProperty("IdentityNumber").Should().BeNull();
        typeof(Person).GetProperty("Nationality").Should().BeNull();
        typeof(Person).GetMethod("ChangeNationality").Should().BeNull();
    }

    [Fact]
    public void Create_Should_RejectBirthPlaceOutsideBirthCountry()
    {
        var result = Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(2005, 1, 2)),
            Country.Create("FR", "France"),
            Location.Create(Country.Create("TR", "Türkiye"), "Rize"));

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be("PERSON_BIRTH_PLACE_COUNTRY_MISMATCH");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangeBirthPlace_Should_RejectAnotherCountry_WithoutChangingExistingState(bool knownPlace)
    {
        var country = Country.Create("FR", "France");
        var original = knownPlace ? Location.Create(country, "Paris") : null;
        var person = Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(2005, 1, 2)), country, original).Value!;

        Action act = () => person.ChangeBirthPlace(Location.Create(Country.Create("TR", "Türkiye"), "Rize"));

        act.Should().Throw<ArgumentException>();
        person.BirthCountry.Should().Be(country);
        person.BirthPlace.Should().Be(original);
    }

    [Fact]
    public void ChangeBirthPlace_Should_AcceptSameCountryCode_WithDifferentDisplayName()
    {
        var person = Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(2005, 1, 2)),
            Country.Create("FR", "France")).Value!;
        var place = Location.Create(Country.Create("fr", "Fransa"), "Paris");

        person.ChangeBirthPlace(place);

        person.BirthPlace.Should().Be(place);
        person.BirthCountry.Code.Should().Be("FR");
    }

    [Fact]
    public void AdditionalCitizenshipAndDocument_Should_PreservePersonBirthCountryAndCard()
    {
        var france = Country.Create("FR", "France");
        var turkey = Country.Create("TR", "Türkiye");
        var person = Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(2005, 1, 2)), france).Value!;
        var personId = person.Id;
        var card = AtmacaCard.Issue(personId, AtmacaCardNumber.Create("ATM-000001").Value!,
            new DateTime(2026, 9, 16, 10, 0, 0, DateTimeKind.Utc)).Value!;
        var cardId = card.Id;
        var firstCitizenship = PersonCitizenship.Register(personId, france, new DateOnly(2005, 1, 2)).Value!;
        var passport = PersonIdentityDocument.Register(personId,
            IdentityNumber.Create("FR", IdentityType.Passport, "TEST12345").Value!,
            new DateOnly(2010, 1, 1), new DateOnly(2020, 1, 1)).Value!;

        var secondCitizenship = PersonCitizenship.Register(personId, turkey, new DateOnly(2020, 4, 15));
        var newDocument = PersonIdentityDocument.Register(personId,
            IdentityNumber.Create("TR", IdentityType.NationalId, "TEST67890").Value!,
            new DateOnly(2020, 4, 15), new DateOnly(2030, 4, 15));

        secondCitizenship.IsSuccess.Should().BeTrue();
        newDocument.IsSuccess.Should().BeTrue();
        secondCitizenship.Value!.PersonId.Should().Be(personId);
        newDocument.Value!.PersonId.Should().Be(personId);
        firstCitizenship.Country.Should().Be(france);
        passport.IdentityNumber.CountryCode.Should().Be("FR");
        person.Id.Should().Be(personId);
        person.BirthCountry.Should().Be(france);
        card.PersonId.Should().Be(personId);
        card.Id.Should().Be(cardId);
    }
}
