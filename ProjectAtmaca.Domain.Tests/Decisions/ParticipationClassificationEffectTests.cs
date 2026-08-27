using FluentAssertions;

using ProjectAtmaca.Domain.Decisions.Effects.Participations;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class ParticipationClassificationEffectTests
{
    [Fact]
    public void Present_Should_CreatePresentClassificationEffect()
    {
        // Act
        ParticipationClassificationEffect effect =
            ParticipationClassificationEffect.Present();

        // Assert
        effect.Outcome
            .Should()
            .Be(ParticipationClassificationOutcome.Present);
    }

    [Fact]
    public void Absent_Should_CreateAbsentClassificationEffect()
    {
        // Act
        ParticipationClassificationEffect effect =
            ParticipationClassificationEffect.Absent();

        // Assert
        effect.Outcome
            .Should()
            .Be(ParticipationClassificationOutcome.Absent);
    }

    [Fact]
    public void Effects_Should_BeEqual_WhenOutcomesAreEqual()
    {
        // Arrange
        ParticipationClassificationEffect first =
            ParticipationClassificationEffect.Present();

        ParticipationClassificationEffect second =
            ParticipationClassificationEffect.Present();

        // Assert
        first.Should()
            .Be(second);
    }
}
