using FluentAssertions;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;

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
