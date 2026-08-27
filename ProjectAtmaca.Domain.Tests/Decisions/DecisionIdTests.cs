using FluentAssertions;

using ProjectAtmaca.Domain.Decisions;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionIdTests
{
    [Fact]
    public void From_Should_ThrowArgumentException_WhenValueIsEmpty()
    {
        // Arrange
        Guid value =
            Guid.Empty;

        // Act
        Action act =
            () => DecisionId.From(value);

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
        DecisionId decisionId =
            DecisionId.From(value);

        // Assert
        decisionId.Value
            .Should()
            .Be(value);
    }

    [Fact]
    public void New_Should_CreateNonEmptyValue()
    {
        // Act
        DecisionId decisionId =
            DecisionId.New();

        // Assert
        decisionId.Value
            .Should()
            .NotBe(Guid.Empty);
    }

    [Fact]
    public void DecisionIds_Should_BeEqual_WhenValuesAreEqual()
    {
        // Arrange
        Guid value =
            Guid.NewGuid();

        DecisionId first =
            DecisionId.From(value);

        DecisionId second =
            DecisionId.From(value);

        // Assert
        first.Should()
            .Be(second);
    }
}
