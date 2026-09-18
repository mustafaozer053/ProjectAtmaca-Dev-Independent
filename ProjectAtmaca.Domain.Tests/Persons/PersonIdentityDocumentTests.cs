using FluentAssertions;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PersonIdentityDocumentTests
{
    [Theory]
    [InlineData(IdentityType.Passport)]
    [InlineData(IdentityType.NationalId)]
    public void Register_Should_PreservePersonIdentityAndDocumentDates(IdentityType type)
    {
        var personId = Guid.NewGuid();
        var identity = IdentityNumber.Create("FR", type, "TEST12345").Value!;
        var issuedOn = new DateOnly(2020, 3, 4);
        var expiresOn = new DateOnly(2030, 3, 4);

        var result = PersonIdentityDocument.Register(personId, identity, issuedOn, expiresOn);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        var document = result.Value!;
        document.PersonId.Should().Be(personId);
        document.IdentityNumber.Should().Be(identity);
        document.IssuedOn.Should().Be(issuedOn);
        document.ExpiresOn.Should().Be(expiresOn);
        document.CreatedByActorId.Should().BeNull();
    }

    [Fact]
    public void Register_Should_RejectEmptyPersonId()
    {
        var result = PersonIdentityDocument.Register(Guid.Empty, Identity(), IssuedOn, ExpiresOn);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonIdentityDocumentErrors.PersonRequired);
    }

    [Fact]
    public void Register_Should_RejectMissingIdentityNumber()
    {
        var result = PersonIdentityDocument.Register(Guid.NewGuid(), null!, IssuedOn, ExpiresOn);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonIdentityDocumentErrors.IdentityNumberRequired);
    }

    [Theory]
    [InlineData(IdentityType.ResidencePermit)]
    [InlineData(IdentityType.ForeignIdentityCard)]
    public void Register_Should_RejectTypesOutsideTheDocumentContract(IdentityType type)
    {
        var identity = IdentityNumber.Create("FR", type, "TEST12345").Value!;
        var result = PersonIdentityDocument.Register(Guid.NewGuid(), identity, IssuedOn, ExpiresOn);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonIdentityDocumentErrors.DocumentTypeUnsupported);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Register_Should_RejectMissingDates(bool missingIssueDate)
    {
        var result = PersonIdentityDocument.Register(
            Guid.NewGuid(), Identity(),
            missingIssueDate ? default : IssuedOn,
            missingIssueDate ? ExpiresOn : default);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(missingIssueDate
            ? PersonIdentityDocumentErrors.IssueDateRequired
            : PersonIdentityDocumentErrors.ExpiryDateRequired);
    }

    [Fact]
    public void Register_Should_RejectExpiryBeforeIssueDate()
    {
        var result = PersonIdentityDocument.Register(Guid.NewGuid(), Identity(), ExpiresOn, IssuedOn);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonIdentityDocumentErrors.DateRangeInvalid);
    }

    [Fact]
    public void Register_Should_AllowSameDayDates()
    {
        var result = PersonIdentityDocument.Register(Guid.NewGuid(), Identity(), IssuedOn, IssuedOn);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IssuedOn.Should().Be(IssuedOn);
        result.Value.ExpiresOn.Should().Be(IssuedOn);
    }

    [Theory]
    [InlineData("TEST12345")]
    [InlineData("NEW12345")]
    public void RegisterAnotherDocument_Should_PreserveEarlierRecord(string newNumber)
    {
        var personId = Guid.NewGuid();
        var original = PersonIdentityDocument.Register(personId, Identity(), IssuedOn, ExpiresOn).Value!;
        var originalId = original.Id;
        var renewedIdentity = IdentityNumber.Create("FR", IdentityType.Passport, newNumber).Value!;
        var renewal = PersonIdentityDocument.Register(
            personId, renewedIdentity, ExpiresOn, ExpiresOn.AddYears(5));

        renewal.IsSuccess.Should().BeTrue();
        renewal.Value!.Id.Should().NotBe(originalId);
        renewal.Value.PersonId.Should().Be(personId);
        renewal.Value.IdentityNumber.Should().Be(renewedIdentity);
        original.Id.Should().Be(originalId);
        original.IdentityNumber.Should().Be(Identity());
        original.IssuedOn.Should().Be(IssuedOn);
        original.ExpiresOn.Should().Be(ExpiresOn);
    }

    private static IdentityNumber Identity() =>
        IdentityNumber.Create("FR", IdentityType.Passport, "TEST12345").Value!;

    private static DateOnly IssuedOn => new(2000, 3, 4);
    private static DateOnly ExpiresOn => new(2010, 3, 4);
}
