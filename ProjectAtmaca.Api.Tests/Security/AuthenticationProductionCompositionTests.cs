using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectAtmaca.Api.Tests.Security;

public sealed class AuthenticationProductionCompositionTests
{
    [Fact]
    public async Task ProductionHost_Should_RequireAuthentication_ForControllerEndpointsByDefault()
    {
        // Arrange
        using WebApplicationFactory<global::Program> rootFactory =
            new();

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