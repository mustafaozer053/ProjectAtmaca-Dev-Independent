using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ProjectAtmaca.Api.Tests.Observability;

public sealed class HealthProductionCompositionTests
{
    [Fact]
    public async Task ProductionHost_Should_ExposeProcessLivenessEndpoint()
    {
        // Arrange
        using WebApplicationFactory<global::Program> factory =
            new ProjectAtmacaApiFactory();

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri(
                            "https://localhost")
                });

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/health/live",
                TestContext.Current.CancellationToken);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType?
            .MediaType
            .Should()
            .Be("text/plain");

        responseBody
            .Should()
            .Be("Healthy");
    }

    [Fact]
    public void ProductionComposition_Should_RegisterDatabaseReadinessCheck()
    {
        // Arrange
        using WebApplicationFactory<Program> factory =
            new ProjectAtmacaApiFactory();

        // Act
        IOptions<HealthCheckServiceOptions> healthCheckOptions =
            factory.Services
                .GetRequiredService<
                    IOptions<HealthCheckServiceOptions>>();

        HealthCheckRegistration? databaseReadinessCheck =
            healthCheckOptions.Value.Registrations
                .SingleOrDefault(
                    registration =>
                        registration.Name ==
                        "projectatmaca-database");

        // Assert
        databaseReadinessCheck
            .Should()
            .NotBeNull();

        databaseReadinessCheck!.Tags
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    "ready"
                });
    }

    [Fact]
    public async Task ProductionHost_Should_KeepLivenessHealthy_AndReportReadinessUnhealthy_WhenDatabaseIsUnavailable()
    {
        // Arrange
        Dictionary<string, string?> unavailableDatabaseConfiguration =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    "Server=127.0.0.1,1;"
                    + "Database=ProjectAtmaca_Unavailable;"
                    + "User Id=sa;"
                    + "Password=ProjectAtmaca_Unavailable_123!;"
                    + "Encrypt=False;"
                    + "Connect Timeout=1;"
                    + "ConnectRetryCount=0"
            };

        using WebApplicationFactory<Program> rootFactory =
            new ProjectAtmacaApiFactory();

        using WebApplicationFactory<Program> factory =
            rootFactory.WithWebHostBuilder(
                builder =>
                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                            configuration.AddInMemoryCollection(
                                unavailableDatabaseConfiguration)));

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri(
                            "https://localhost")
                });

        // Act
        using HttpResponseMessage livenessResponse =
            await client.GetAsync(
                "/health/live",
                TestContext.Current.CancellationToken);

        using HttpResponseMessage readinessResponse =
            await client.GetAsync(
                "/health/ready",
                TestContext.Current.CancellationToken);

        string readinessResponseBody =
            await readinessResponse.Content
                .ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

        // Assert
        livenessResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        readinessResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.ServiceUnavailable);

        readinessResponseBody
            .Should()
            .Be("Unhealthy");
    }
}