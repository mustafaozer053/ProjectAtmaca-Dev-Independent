using FluentAssertions;

using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionApplicationCreationTests
{
    [Fact]
    public void Create_Should_PreserveProvenanceFacts()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        ParticipationId participationId =
            ParticipationId.New();

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                participationId);

        DecisionRevision appliedDecisionRevision =
            DecisionRevision.From(3);

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                27,
                10,
                30,
                0,
                TimeSpan.Zero);

        // Act
        DecisionApplication application =
            DecisionApplication.Create(
                decisionId,
                target,
                appliedDecisionRevision,
                appliedAtUtc);

        // Assert
        application.DecisionApplicationId.Value
            .Should()
            .NotBe(Guid.Empty);

        application.DecisionId
            .Should()
            .Be(decisionId);

        application.Target
            .Should()
            .Be(target);

        application.AppliedDecisionRevision
            .Should()
            .Be(appliedDecisionRevision);

        application.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_WhenDecisionIdIsDefault()
    {
        // Arrange
        DecisionId decisionId =
            default;

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                ParticipationId.New());

        DecisionRevision appliedDecisionRevision =
            DecisionRevision.Initial;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                27,
                10,
                30,
                0,
                TimeSpan.Zero);

        // Act
        Action act =
            () => DecisionApplication.Create(
                decisionId,
                target,
                appliedDecisionRevision,
                appliedAtUtc);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("decisionId");
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_WhenTargetIsDefault()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        DecisionTargetReference target =
            default;

        DecisionRevision appliedDecisionRevision =
            DecisionRevision.Initial;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                27,
                10,
                30,
                0,
                TimeSpan.Zero);

        // Act
        Action act =
            () => DecisionApplication.Create(
                decisionId,
                target,
                appliedDecisionRevision,
                appliedAtUtc);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("target");
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_WhenAppliedDecisionRevisionIsDefault()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                ParticipationId.New());

        DecisionRevision appliedDecisionRevision =
            default;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                27,
                10,
                30,
                0,
                TimeSpan.Zero);

        // Act
        Action act =
            () => DecisionApplication.Create(
                decisionId,
                target,
                appliedDecisionRevision,
                appliedAtUtc);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName(
                "appliedDecisionRevision");
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_WhenAppliedAtUtcIsDefault()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                ParticipationId.New());

        DecisionRevision appliedDecisionRevision =
            DecisionRevision.Initial;

        DateTimeOffset appliedAtUtc =
            default;

        // Act
        Action act =
            () => DecisionApplication.Create(
                decisionId,
                target,
                appliedDecisionRevision,
                appliedAtUtc);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("appliedAtUtc");
    }

    [Fact]
    public void Create_Should_ThrowArgumentException_WhenAppliedAtUtcHasNonZeroOffset()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                ParticipationId.New());

        DecisionRevision appliedDecisionRevision =
            DecisionRevision.Initial;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                27,
                10,
                30,
                0,
                TimeSpan.FromHours(3));

        // Act
        Action act =
            () => DecisionApplication.Create(
                decisionId,
                target,
                appliedDecisionRevision,
                appliedAtUtc);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("appliedAtUtc");
    }
}
