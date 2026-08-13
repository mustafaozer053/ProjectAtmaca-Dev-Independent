using FluentAssertions;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Participations;

public sealed class ParticipationCorrectionReasonTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_ReasonIsMissing(
        string? value)
    {
        // Act
        Result<ParticipationCorrectionReason> result =
            ParticipationCorrectionReason.Create(
                value);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Value.Should().BeNull();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .CorrectionReasonRequired);
    }

    [Fact]
    public void Create_Should_Trim_Reason()
    {
        // Act
        Result<ParticipationCorrectionReason> result =
            ParticipationCorrectionReason.Create(
                "  Giriş saati yanlış kaydedildi.  ");

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().NotBeNull();

        result.Value!.Value.Should()
            .Be("Giriş saati yanlış kaydedildi.");
    }

    [Fact]
    public void Create_Should_Fail_When_ReasonExceedsMaximumLength()
    {
        // Arrange
        string value =
            new(
                'A',
                ParticipationCorrectionReason.MaxLength + 1);

        // Act
        Result<ParticipationCorrectionReason> result =
            ParticipationCorrectionReason.Create(
                value);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Value.Should().BeNull();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .CorrectionReasonTooLong);
    }
}
