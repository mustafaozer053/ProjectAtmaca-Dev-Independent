using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProjectAtmaca.Api.Tests.Security;

public sealed class AuthenticationProductionCompositionTests
{
    [Fact]
    public async Task ProductionHost_Should_RequireAuthentication_ForControllerEndpointsByDefault()
    {
        // Arrange
        using WebApplicationFactory<global::Program> rootFactory =
            new ProjectAtmacaApiFactory();

        using WebApplicationFactory<global::Program> factory =
            rootFactory.WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureServices(
                        services =>
                        {
                            services
                                .AddControllers()
                                .AddApplicationPart(
                                    typeof(
                                        AuthenticationProbeController)
                                    .Assembly);
                        });
                });

        using HttpClient client =
            factory.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/__security/authentication-probe",
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Authentication:Authority")]
    [InlineData("Authentication:Audience")]
    public void ProductionHost_Should_FailAtStartup_WhenRequiredAuthenticationConfigurationIsMissing(
        string missingConfigurationKey)
    {
        // Arrange
        Dictionary<string, string?> configurationValues =
            new()
            {
                ["Authentication:Authority"] =
                    "https://identity.projectatmaca.test",
                ["Authentication:Audience"] =
                    "project-atmaca-api"
            };

        configurationValues[missingConfigurationKey] =
            string.Empty;

        using WebApplicationFactory<global::Program> rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            rootFactory.WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureAppConfiguration(
                        (_, configuration) =>
                        {
                            configuration
                                .AddInMemoryCollection(
                                    configurationValues);
                        });
                });

        // Act
        Action startHost =
            () =>
            {
                using HttpClient client =
                    factory.CreateClient();
            };

        // Assert
        startHost
            .Should()
            .Throw<OptionsValidationException>()
            .WithMessage(
                $"*{missingConfigurationKey}*");
    }
}

[ApiController]
[Route("__security/authentication-probe")]
public sealed class AuthenticationProbeController
    : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}