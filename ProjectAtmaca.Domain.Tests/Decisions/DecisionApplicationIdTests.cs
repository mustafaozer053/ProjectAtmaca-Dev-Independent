using FluentAssertions;

using ProjectAtmaca.Domain.Decisions;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionApplicationIdTests
{
    [Fact]
    public void From_Should_ThrowArgumentException_WhenValueIsEmpty()
    {
        // Arrange
        Guid value =
            Guid.Empty;

        // Act
        Action act =
            () => DecisionApplicationId.From(
                value);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void From_Should_PreserveProvidedValue()
    {
        // Arrange
        Guid value =
            Guid.NewGuid();

        // Act
        DecisionApplicationId id =
            DecisionApplicationId.From(
                value);

        // Assert
        id.Value
            .Should()
            .Be(value);
    }

    [Fact]
    public void New_Should_CreateNonEmptyId()
    {
        // Act
        DecisionApplicationId id =
            DecisionApplicationId.New();

        // Assert
        id.Value
            .Should()
            .NotBe(Guid.Empty);
    }

    [Fact]
    public void New_Should_CreateDistinctIds()
    {
        // Act
        DecisionApplicationId first =
            DecisionApplicationId.New();

        DecisionApplicationId second =
            DecisionApplicationId.New();

        // Assert
        first.Should()
            .NotBe(second);
    }
}
