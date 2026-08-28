using FluentAssertions;

using ProjectAtmaca.Domain.Decisions;

using Xunit;

namespace ProjectAtmaca.Domain.Tests.Decisions;

public sealed class DecisionApplicationBoundaryTests
{
    [Fact]
    public void DecisionApplication_Should_NotHoldDecisionAggregateReference()
    {
        // Arrange
        var decisionProperties =
            typeof(DecisionApplication)
                .GetProperties()
                .Where(property =>
                    property.PropertyType ==
                    typeof(Decision))
                .ToArray();

        // Assert
        decisionProperties
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void DecisionApplicationCreation_Should_NotRequireDecisionAggregate()
    {
        // Arrange
        var publicStaticMethods =
            typeof(DecisionApplication)
                .GetMethods(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static);

        var methodsRequiringDecision =
            publicStaticMethods
                .Where(method =>
                    method.GetParameters()
                        .Any(parameter =>
                            parameter.ParameterType ==
                            typeof(Decision)))
                .ToArray();

        // Assert
        methodsRequiringDecision
            .Should()
            .BeEmpty();
    }
}
