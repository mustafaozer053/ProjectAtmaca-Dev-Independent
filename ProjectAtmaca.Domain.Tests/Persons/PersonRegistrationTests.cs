using FluentAssertions;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PersonRegistrationTests
{
    [Theory]
    [InlineData(TurkishCitizenshipStatus.ByBirth)]
    [InlineData(TurkishCitizenshipStatus.Acquired)]
    [InlineData(TurkishCitizenshipStatus.NotTurkishCitizen)]
    public void Record_Should_PreservePersonAndAcceptedIdentity(TurkishCitizenshipStatus status)
    {
        var personId = Guid.NewGuid();
        var identity = RegistrationIdentity.Create(status,
            status == TurkishCitizenshipStatus.NotTurkishCitizen ? null : "12345678901",
            status == TurkishCitizenshipStatus.NotTurkishCitizen ? "AB12345" : null,
            status == TurkishCitizenshipStatus.Acquired ? new DateOnly(2020, 6, 12) : null).Value!;

        var result = PersonRegistration.Record(personId, identity);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PersonId.Should().Be(personId);
        result.Value.Identity.Should().BeSameAs(identity);
        result.Value.Id.Should().NotBeEmpty();
        result.Value.CreatedByActorId.Should().BeNull();
    }

    [Fact]
    public void Record_Should_RejectMissingPerson()
    {
        var identity = RegistrationIdentity.Create(TurkishCitizenshipStatus.ByBirth, "12345678901").Value!;
        var result = PersonRegistration.Record(Guid.Empty, identity);
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonRegistrationErrors.PersonRequired);
    }

    [Fact]
    public void Record_Should_RejectMissingIdentity()
    {
        var result = PersonRegistration.Record(Guid.NewGuid(), null!);
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(PersonRegistrationErrors.IdentityRequired);
    }
}
