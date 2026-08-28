using FluentAssertions;
using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionApplicationImmutabilityTests
{
    [Theory]
    [InlineData(nameof(DecisionApplication.DecisionId))]
    [InlineData(nameof(DecisionApplication.Target))]
    [InlineData(nameof(DecisionApplication.AppliedDecisionRevision))]
    [InlineData(nameof(DecisionApplication.AppliedAtUtc))]
    public void ProvenanceProperties_Should_NotExposePublicSetters(
        string propertyName)
    {
        // Arrange
        var property =
            typeof(DecisionApplication)
                .GetProperty(propertyName);

        // Assert
        property.Should().NotBeNull();

        property!.SetMethod?
            .IsPublic
            .Should()
            .BeFalse();
    }

    [Fact]
    public void DecisionApplication_Should_NotExposePublicInstanceMutationMethods()
    {
        // Arrange
        var allowedMethods =
            new HashSet<string>
            {
            nameof(object.Equals),
            nameof(object.GetHashCode),
            nameof(object.GetType),
            nameof(object.ToString)
            };

        var publicInstanceMethods =
            typeof(DecisionApplication)
                .GetMethods(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public)
                .Where(method =>
                    !method.IsSpecialName &&
                    !allowedMethods.Contains(method.Name))
                .ToArray();

        // Assert
        publicInstanceMethods
            .Should()
            .BeEmpty();
    }
}
