using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Auditing;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests.Auditing;

public sealed class
    AuditableEntitySaveChangesInterceptorProductionCompositionTests
{
    [Fact]
    public void ProductionInfrastructure_Should_RegisterScopedAuditInterceptor_AndAttachItToDbContextOptions()
    {
        ServiceCollection services =
            new();

        services.AddScoped<ICurrentActor>(
            _ =>
                new StubCurrentActor(
                    ActorId.New()));

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [
                            "ConnectionStrings:" +
                            "ProjectAtmacaDatabase"
                        ] =
                            "Server=(localdb)\\mssqllocaldb;" +
                            "Database=ProjectAtmaca_CompositionTests;" +
                            "Trusted_Connection=True;" +
                            "TrustServerCertificate=True"
                    })
                .Build();

        services.AddInfrastructure(
            configuration);

        ServiceDescriptor? descriptor =
            services.SingleOrDefault(
                candidate =>
                    candidate.ServiceType ==
                    typeof(
                        AuditableEntitySaveChangesInterceptor));

        descriptor
            .Should()
            .NotBeNull(
                "production infrastructure must register " +
                "canonical audit stamping");

        descriptor!
            .Lifetime
            .Should()
            .Be(
                ServiceLifetime.Scoped,
                "audit attribution depends on the scoped current actor");

        descriptor
            .ImplementationType
            .Should()
            .Be(
                typeof(
                    AuditableEntitySaveChangesInterceptor));

        using ServiceProvider provider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

        using IServiceScope scope =
            provider.CreateScope();

        AuditableEntitySaveChangesInterceptor interceptor =
            scope.ServiceProvider
                .GetRequiredService<
                    AuditableEntitySaveChangesInterceptor>();

        DbContextOptions<ProjectAtmacaDbContext> options =
            scope.ServiceProvider
                .GetRequiredService<
                    DbContextOptions<ProjectAtmacaDbContext>>();

        IInterceptor[] configuredInterceptors =
            options.Extensions
                .OfType<CoreOptionsExtension>()
                .SelectMany(
                    extension =>
                        extension.Interceptors ??
                        Array.Empty<IInterceptor>())
                .ToArray();

        configuredInterceptors
            .Should()
            .ContainSingle(
                configuredInterceptor =>
                    ReferenceEquals(
                        configuredInterceptor,
                        interceptor),
                "the scoped interceptor must be attached to " +
                "the production ProjectAtmacaDbContext options");
    }

    private sealed class StubCurrentActor :
        ICurrentActor
    {
        public StubCurrentActor(
            ActorId actorId)
        {
            ActorId = actorId;
        }

        public bool IsAuthenticated =>
            true;

        public ActorId ActorId { get; }
    }
}