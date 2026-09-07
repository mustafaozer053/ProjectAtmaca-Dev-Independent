using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionApplicationProductionCompositionTests
{
    [Fact]
    public async Task ProductionComposition_Should_ResolveApplyParticipationClassificationHandler()
    {
        // Arrange
        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddLogging();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        await using AsyncServiceScope scope =
            serviceProvider.CreateAsyncScope();

        // Act
        Func<ApplyParticipationClassificationCommandHandler>
            action =
                () =>
                    scope.ServiceProvider
                        .GetRequiredService<
                            ApplyParticipationClassificationCommandHandler>();

        // Assert
        action.Should()
            .NotThrow();
    }
}
