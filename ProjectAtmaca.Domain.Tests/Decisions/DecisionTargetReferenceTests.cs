using FluentAssertions;

using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionTargetReferenceTests
{
    [Fact]
    public void ForParticipation_Should_CreateReferenceForProvidedParticipation()
    {
        // Arrange
        ParticipationId participationId =
            ParticipationId.New();

        // Act
        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                participationId);

        // Assert
        target.TargetType
            .Should()
            .Be(DecisionTargetType.Participation);

        target.TargetId
            .Should()
            .Be(participationId.Value);
    }

    [Fact]
    public void References_Should_BeEqual_WhenTheyTargetSameParticipation()
    {
        // Arrange
        ParticipationId participationId =
            ParticipationId.New();

        // Act
        DecisionTargetReference first =
            DecisionTargetReference.ForParticipation(
                participationId);

        DecisionTargetReference second =
            DecisionTargetReference.ForParticipation(
                participationId);

        // Assert
        first.Should()
            .Be(second);
    }

    [Fact]
    public void References_Should_NotBeEqual_WhenTheyTargetDifferentParticipations()
    {
        // Arrange
        ParticipationId firstParticipationId =
            ParticipationId.New();

        ParticipationId secondParticipationId =
            ParticipationId.New();

        // Act
        DecisionTargetReference first =
            DecisionTargetReference.ForParticipation(
                firstParticipationId);

        DecisionTargetReference second =
            DecisionTargetReference.ForParticipation(
                secondParticipationId);

        // Assert
        first.Should()
            .NotBe(second);
    }
}
