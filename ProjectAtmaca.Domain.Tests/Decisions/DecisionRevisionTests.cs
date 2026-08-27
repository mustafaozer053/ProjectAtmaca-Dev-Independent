using FluentAssertions;

using ProjectAtmaca.Domain.Decisions;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionRevisionTests
{
    [Fact]
    public void From_Should_ThrowArgumentOutOfRangeException_WhenValueIsZero()
    {
        // Arrange
        const int value =
            0;

        // Act
        Action act =
            () => DecisionRevision.From(value);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");
    }

    [Fact]
    public void From_Should_ThrowArgumentOutOfRangeException_WhenValueIsNegative()
    {
        // Arrange
        const int value =
            -1;

        // Act
        Action act =
            () => DecisionRevision.From(value);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");
    }

    [Fact]
    public void Initial_Should_HaveValueOne()
    {
        // Act
        DecisionRevision revision =
            DecisionRevision.Initial;

        // Assert
        revision.Value
            .Should()
            .Be(1);
    }

    [Fact]
    public void From_Should_PreserveProvidedValue()
    {
        // Arrange
        const int value =
            7;

        // Act
        DecisionRevision revision =
            DecisionRevision.From(value);

        // Assert
        revision.Value
            .Should()
            .Be(value);
    }

    [Fact]
    public void Next_Should_AdvanceRevisionByOne()
    {
        // Arrange
        DecisionRevision revision =
            DecisionRevision.From(7);

        // Act
        DecisionRevision nextRevision =
            revision.Next();

        // Assert
        nextRevision.Value
            .Should()
            .Be(8);
    }

    [Fact]
    public void DecisionRevisions_Should_BeEqual_WhenValuesAreEqual()
    {
        // Arrange
        DecisionRevision first =
            DecisionRevision.From(7);

        DecisionRevision second =
            DecisionRevision.From(7);

        // Assert
        first.Should()
            .Be(second);
    }
}
