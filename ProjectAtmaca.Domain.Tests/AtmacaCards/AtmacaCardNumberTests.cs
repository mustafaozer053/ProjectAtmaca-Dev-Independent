using FluentAssertions;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Tests.AtmacaCards;

public sealed class AtmacaCardNumberTests
{
    [Theory]
    [InlineData("ATM-000001", "ATM-000001")]
    [InlineData("ATM-999999", "ATM-999999")]
    [InlineData("ATM-000010", "ATM-000010")]
    [InlineData("atm-000001", "ATM-000001")]
    [InlineData(" \tatm-000001\r\n", "ATM-000001")]
    public void Create_Should_PreserveCanonicalNumber(string input, string expected)
    {
        var result = AtmacaCardNumber.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value!.Value.Should().Be(expected);
        result.Value.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "ATMACA_CARD_NUMBER_REQUIRED")]
    [InlineData("", "ATMACA_CARD_NUMBER_REQUIRED")]
    [InlineData(" \t\r\n", "ATMACA_CARD_NUMBER_REQUIRED")]
    [InlineData("000001", "ATMACA_CARD_NUMBER_INVALID_PREFIX")]
    [InlineData("XYZ-000001", "ATMACA_CARD_NUMBER_INVALID_PREFIX")]
    [InlineData("ATM-", "ATMACA_CARD_NUMBER_INVALID_FORMAT")]
    [InlineData("ATM-00001", "ATMACA_CARD_NUMBER_INVALID_FORMAT")]
    [InlineData("ATM-1000000", "ATMACA_CARD_NUMBER_INVALID_FORMAT")]
    [InlineData("ATM-00000A", "ATMACA_CARD_NUMBER_INVALID_FORMAT")]
    [InlineData("ATM-000 01", "ATMACA_CARD_NUMBER_INVALID_FORMAT")]
    [InlineData("ATM-+00001", "ATMACA_CARD_NUMBER_INVALID_FORMAT")]
    [InlineData("ATM--00001", "ATMACA_CARD_NUMBER_INVALID_FORMAT")]
    [InlineData("ATM-000000", "ATMACA_CARD_NUMBER_INVALID_SEQUENCE")]
    public void Create_Should_RejectInvalidNumber_WithoutReturningAValue(
        string? input, string expectedCode)
    {
        var result = AtmacaCardNumber.Create(input!);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData("ATM-\u0660\u0660\u0660\u0660\u0660\u0661")]
    [InlineData("ATM-\uff10\uff10\uff10\uff10\uff10\uff11")]
    public void Create_Should_RejectNonAsciiDigitLookalikes(string input)
    {
        var result = AtmacaCardNumber.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public void NormalizedNumbers_Should_RepresentTheSameValue()
    {
        var canonical = AtmacaCardNumber.Create("ATM-000001").Value!;
        var normalized = AtmacaCardNumber.Create(" atm-000001 ").Value!;
        var different = AtmacaCardNumber.Create("ATM-000002").Value!;

        canonical.Equals(normalized).Should().BeTrue();
        canonical.GetHashCode().Should().Be(normalized.GetHashCode());
        canonical.Equals(different).Should().BeFalse();
    }
}
