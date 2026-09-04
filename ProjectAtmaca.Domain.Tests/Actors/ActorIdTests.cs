using FluentAssertions;

using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Domain.Tests.Actors;

public sealed class ActorIdTests
{
    [Fact]
    public void From_Should_ThrowArgumentException_WhenValueIsEmpty()
    {
        // Arrange
        Guid value =
            Guid.Empty;

        // Act
        Action createActorId =
            () => ActorId.From(
                value);

        // Assert
        createActorId
            .Should()
            .Throw<ArgumentException>()
            .WithParameterName(
                "value");
    }

    [Fact]
    public void From_Should_PreserveProvidedValue()
    {
        // Arrange
        Guid value =
            Guid.NewGuid();

        // Act
        ActorId actorId =
            ActorId.From(
                value);

        // Assert
        actorId.Value
            .Should()
            .Be(value);
    }

    [Fact]
    public void New_Should_CreateNonEmptyValue()
    {
        // Act
        ActorId actorId =
            ActorId.New();

        // Assert
        actorId.Value
            .Should()
            .NotBe(Guid.Empty);
    }

    [Fact]
    public void ActorIds_Should_BeEqual_WhenValuesAreEqual()
    {
        // Arrange
        Guid value =
            Guid.NewGuid();

        ActorId first =
            ActorId.From(
                value);

        ActorId second =
            ActorId.From(
                value);

        // Act
        bool areEqual =
            first == second;

        // Assert
        areEqual
            .Should()
            .BeTrue();
    }
}