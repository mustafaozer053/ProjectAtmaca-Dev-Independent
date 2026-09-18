using FluentAssertions;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PersonCreationTests
{
    [Fact]
    public void Create_Should_AllowMissingParentNamesAndBirthPlace()
    {
        var result = Person.Create(Name(), Birth(), Country());

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value!.MotherName.Should().BeNull();
        result.Value.FatherName.Should().BeNull();
        result.Value.BirthPlace.Should().BeNull();
        result.Value.Name.Should().Be(Name());
        result.Value.BirthCountry.Should().Be(Country());
        result.Value.BirthDate.Should().Be(Birth());
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Create_Should_PreserveEachKnownOptionalDetail(
        bool hasBirthPlace, bool hasMother, bool hasFather)
    {
        var place = hasBirthPlace ? Location.Create(Country(), "Rize") : null;
        var mother = hasMother ? PersonName.Create("Test Mother").Value! : null;
        var father = hasFather ? PersonName.Create("Test Father").Value! : null;

        var result = Person.Create(Name(), Birth(), Country(), place, mother, father);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BirthPlace.Should().Be(place);
        result.Value.MotherName.Should().Be(mother);
        result.Value.FatherName.Should().Be(father);
    }

    [Fact]
    public void MissingDetails_Should_BeCompleted_WithoutReplacingThePerson()
    {
        var person = Person.Create(Name(), Birth(), Country()).Value!;
        var id = person.Id;
        var createdAt = person.CreatedAtUtc;
        var cardResult = AtmacaCard.Issue(
            id,
            AtmacaCardNumber.Create("ATM-000001").Value!,
            new DateTime(2026, 9, 16, 10, 0, 0, DateTimeKind.Utc));
        cardResult.IsSuccess.Should().BeTrue();
        var card = cardResult.Value!;
        var cardId = card.Id;
        var place = Location.Create(Country(), "Rize");
        var mother = PersonName.Create("Test Mother").Value!;
        var father = PersonName.Create("Test Father").Value!;

        person.ChangeBirthPlace(place);
        person.ChangeMotherName(mother);
        person.ChangeFatherName(father);

        person.Id.Should().Be(id);
        person.CreatedAtUtc.Should().Be(createdAt);
        person.BirthCountry.Should().Be(Country());
        person.BirthPlace.Should().Be(place);
        person.MotherName.Should().Be(mother);
        person.FatherName.Should().Be(father);
        card.PersonId.Should().Be(person.Id);
        card.Id.Should().Be(cardId);
    }

    [Theory]
    [InlineData("name", "PERSON_NAME_REQUIRED")]
    [InlineData("birthCountry", "PERSON_BIRTH_COUNTRY_REQUIRED")]
    [InlineData("birthDate", "PERSON_BIRTH_DATE_REQUIRED")]
    public void Create_Should_RejectMissingCoreFields(string missing, string code)
    {
        var result = Person.Create(
            missing == "name" ? null! : Name(),
            missing == "birthDate" ? null! : Birth(),
            missing == "birthCountry" ? null! : Country());

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be(code);
    }

    private static PersonName Name() => PersonName.Create("Test Person").Value!;
    private static Country Country() =>
        ProjectAtmaca.Domain.Common.ValueObjects.Country.Create("TR", "Türkiye");
    private static BirthDate Birth() => BirthDate.Create(new DateTime(2005, 1, 2));
}
