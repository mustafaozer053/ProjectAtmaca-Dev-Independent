using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Application.Participations.RecordArrival;
using ProjectAtmaca.Application.Participations.RecordDeparture;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests;

public sealed class DependencyInjectionResolutionTests
{
    [Fact]
    public void ApplicationAndInfrastructureRegistrations_Should_Resolve_CreateParticipationCommandHandler()
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
                .AddInMemoryCollection(configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        using IServiceScope scope =
            serviceProvider.CreateScope();

        // Act
        CreateParticipationCommandHandler? handler =
            scope.ServiceProvider
                .GetService<CreateParticipationCommandHandler>();

        // Assert
        handler.Should()
            .NotBeNull();
    }

    [Fact]
    public void ApplicationAndInfrastructureRegistrations_Should_Resolve_ParticipationReader()
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
                .AddInMemoryCollection(configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        using IServiceScope scope =
            serviceProvider.CreateScope();

        // Act
        IParticipationReader? reader =
            scope.ServiceProvider
                .GetService<IParticipationReader>();

        // Assert
        reader.Should()
            .NotBeNull();

        reader.Should()
            .BeOfType<ParticipationReader>();
    }

    [Fact]
    public void ApplicationAndInfrastructureRegistrations_Should_Resolve_MarkParticipationPresentCommandHandler()
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
                .AddInMemoryCollection(configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        using IServiceScope scope =
            serviceProvider.CreateScope();

        // Act
        MarkParticipationPresentCommandHandler? handler =
            scope.ServiceProvider
                .GetService<
                    MarkParticipationPresentCommandHandler>();

        // Assert
        handler.Should()
            .NotBeNull();
    }

    [Fact]
    public void ApplicationAndInfrastructureRegistrations_Should_Resolve_RecordParticipationArrivalCommandHandler()
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
                .AddInMemoryCollection(configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        using IServiceScope scope =
            serviceProvider.CreateScope();

        // Act
        RecordParticipationArrivalCommandHandler? handler =
            scope.ServiceProvider
                .GetService<
                    RecordParticipationArrivalCommandHandler>();

        // Assert
        handler.Should()
            .NotBeNull();
    }

    [Fact]
    public void ApplicationAndInfrastructureRegistrations_Should_Resolve_RecordParticipationDepartureCommandHandler()
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
                .AddInMemoryCollection(configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        using IServiceScope scope =
            serviceProvider.CreateScope();

        // Act
        RecordParticipationDepartureCommandHandler? handler =
            scope.ServiceProvider
                .GetService<
                    RecordParticipationDepartureCommandHandler>();

        // Assert
        handler
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void ApplicationAndInfrastructureRegistrations_Should_Resolve_ListParticipationsByActivityQueryHandler()
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

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        using IServiceScope scope =
            serviceProvider.CreateScope();

        // Act
        ListParticipationsByActivityQueryHandler? handler =
            scope.ServiceProvider
                .GetService<
                    ListParticipationsByActivityQueryHandler>();

        // Assert
        handler
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void ApplicationAndInfrastructureRegistrations_Should_Resolve_GetParticipationSummaryByActivityQueryHandler()
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

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        using IServiceScope scope =
            serviceProvider.CreateScope();

        // Act
        GetParticipationSummaryByActivityQueryHandler? handler =
            scope.ServiceProvider
                .GetService<
                    GetParticipationSummaryByActivityQueryHandler>();

        // Assert
        handler
            .Should()
            .NotBeNull();
    }
}
