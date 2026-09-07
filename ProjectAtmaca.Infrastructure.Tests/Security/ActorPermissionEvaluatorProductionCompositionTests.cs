using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Infrastructure.Tests.Support;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests.Security;

public sealed class
    ActorPermissionEvaluatorProductionCompositionTests
{
    [Fact]
    public void ProductionInfrastructure_Should_RegisterActorPermissionEvaluatorAsScopedService()
    {
        Dictionary<string, string?> configurationValues =
            new()
            {
                [
                    "ConnectionStrings:" +
                    "ProjectAtmacaDatabase"
                ] =
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ProjectAtmaca_CompositionTests;" +
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

        ServiceDescriptor? descriptor =
            services.SingleOrDefault(
                candidate =>
                    candidate.ServiceType ==
                        typeof(IActorPermissionEvaluator));

        descriptor.Should()
            .NotBeNull(
                "production infrastructure must register " +
                "IActorPermissionEvaluator");

        descriptor!.ImplementationType.Should()
            .Be(
                typeof(ActorPermissionEvaluator));

        descriptor.Lifetime.Should()
            .Be(ServiceLifetime.Scoped);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });

        IActorPermissionEvaluator firstResolution;
        IActorPermissionEvaluator repeatedResolution;

        using (
            IServiceScope firstScope =
                serviceProvider.CreateScope())
        {
            firstResolution =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        IActorPermissionEvaluator>();

            repeatedResolution =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        IActorPermissionEvaluator>();

            repeatedResolution.Should()
                .BeSameAs(firstResolution);

            firstResolution.Should()
                .BeOfType<ActorPermissionEvaluator>();
        }

        using IServiceScope secondScope =
            serviceProvider.CreateScope();

        IActorPermissionEvaluator secondResolution =
            secondScope.ServiceProvider
                .GetRequiredService<
                    IActorPermissionEvaluator>();

        secondResolution.Should()
            .BeOfType<ActorPermissionEvaluator>();

        secondResolution.Should()
            .NotBeSameAs(firstResolution);
    }
}