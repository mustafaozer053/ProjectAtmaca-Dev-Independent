using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Persistence.Security;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests.Security;

public sealed class ActorIdentityResolverProductionCompositionTests
{
    [Fact]
    public void ProductionInfrastructure_Should_RegisterActorIdentityResolverAsScopedService()
    {
        // Arrange
        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ProjectAtmaca_IntegrationTests;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True"
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

        // Act
        ServiceDescriptor? descriptor =
            services.SingleOrDefault(
                candidate =>
                    candidate.ServiceType ==
                        typeof(IActorIdentityResolver));

        // Assert
        descriptor
            .Should()
            .NotBeNull(
                "production infrastructure must register " +
                "IActorIdentityResolver");

        descriptor!.ImplementationType
            .Should()
            .Be(
                typeof(ActorIdentityResolver));

        descriptor.Lifetime
            .Should()
            .Be(
                ServiceLifetime.Scoped);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild =
                        true,
                    ValidateScopes =
                        true
                });

        using IServiceScope firstScope =
            serviceProvider.CreateScope();

        IActorIdentityResolver firstResolution =
            firstScope.ServiceProvider
                .GetRequiredService<IActorIdentityResolver>();

        IActorIdentityResolver repeatedResolution =
            firstScope.ServiceProvider
                .GetRequiredService<IActorIdentityResolver>();

        using IServiceScope secondScope =
            serviceProvider.CreateScope();

        IActorIdentityResolver secondResolution =
            secondScope.ServiceProvider
                .GetRequiredService<IActorIdentityResolver>();

        firstResolution
            .Should()
            .BeOfType<ActorIdentityResolver>();

        firstResolution
            .Should()
            .BeSameAs(
                repeatedResolution);

        firstResolution
            .Should()
            .NotBeSameAs(
                secondResolution);
    }
}