using FluentAssertions;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PersonCitizenshipTests
{
    [Fact]
    public void Register_Should_PreservePersonCountryAndKnownAcquisitionDate()
    {
        var personId = Guid.NewGuid();
        var country = Country.Create("TR", "Türkiye");
        var acquiredOn = new DateOnly(2020, 4, 15);

        var result = PersonCitizenship.Register(personId, country, acquiredOn);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        var citizenship = result.Value!;
        citizenship.PersonId.Should().Be(personId);
        citizenship.Country.Should().Be(country);
        citizenship.AcquiredOn.Should().Be(acquiredOn);
        citizenship.CreatedByActorId.Should().BeNull();
    }

    [Fact]
    public void Register_Should_RejectEmptyPersonId()
    {
        var result = PersonCitizenship.Register(
            Guid.Empty, Country.Create("TR", "Türkiye"), new DateOnly(2020, 4, 15));

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonCitizenshipErrors.PersonRequired);
    }

    [Fact]
    public void Register_Should_RejectMissingCountry()
    {
        var result = PersonCitizenship.Register(Guid.NewGuid(), null!, new DateOnly(2020, 4, 15));

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonCitizenshipErrors.CountryRequired);
    }

    [Theory]
    [InlineData("FR", "France")]
    [InlineData("TR", "Türkiye")]
    public void Register_Should_AllowUnknownDate_ForSupplementaryCitizenship(string code, string name)
    {
        var result = PersonCitizenship.Register(Guid.NewGuid(), Country.Create(code, name));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Country.Code.Should().Be(code);
        result.Value.AcquiredOn.Should().BeNull();
    }

    [Fact]
    public void Register_Should_RejectAnExplicitDefaultDate()
    {
        var result = PersonCitizenship.Register(
            Guid.NewGuid(), Country.Create("TR", "Türkiye"), default(DateOnly));

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonCitizenshipErrors.AcquisitionDateInvalid);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2020)]
    public void Register_Should_AllowConcurrentCountries_WithoutReplacingTheEarlierRecord(int secondYear)
    {
        var personId = Guid.NewGuid();
        var france = Country.Create("FR", "France");
        var turkey = Country.Create("TR", "Türkiye");
        var firstDate = new DateOnly(2000, 4, 15);
        var secondDate = new DateOnly(secondYear, 4, 15);
        var firstResult = PersonCitizenship.Register(personId, france, firstDate);
        firstResult.IsSuccess.Should().BeTrue();
        var first = firstResult.Value!;
        var firstId = first.Id;

        var secondResult = PersonCitizenship.Register(personId, turkey, secondDate);

        secondResult.IsSuccess.Should().BeTrue();
        var second = secondResult.Value!;
        second.Id.Should().NotBe(firstId);
        second.PersonId.Should().Be(first.PersonId);
        second.Country.Should().Be(turkey);
        second.AcquiredOn.Should().Be(secondDate);
        first.Id.Should().Be(firstId);
        first.Country.Should().Be(france);
        first.AcquiredOn.Should().Be(firstDate);
    }
}
