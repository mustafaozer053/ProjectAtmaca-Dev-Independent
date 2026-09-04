using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Application.Tests.Security;

public sealed class CurrentActorContractTests
{
    [Fact]
    public void CurrentActor_Should_ExposeCanonicalActorId()
    {
        // Arrange
        ActorId actorId =
            ActorId.New();

        ICurrentActor currentActor =
            new StubCurrentActor(
                actorId);

        // Act
        ActorId actualActorId =
            currentActor.ActorId;

        // Assert
        actualActorId
            .Should()
            .Be(actorId);
    }

    private sealed class StubCurrentActor
        : ICurrentActor
    {
        public StubCurrentActor(
            ActorId actorId)
        {
            ActorId =
                actorId;
        }

        public ActorId ActorId { get; }
    }
}