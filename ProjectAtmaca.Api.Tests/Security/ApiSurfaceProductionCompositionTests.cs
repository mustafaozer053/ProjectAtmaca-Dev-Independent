using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectAtmaca.Api.Tests.Security;

public sealed class ApiSurfaceProductionCompositionTests
{
    [Fact]
    public async Task ProductionHost_Should_NotExposeTemplateWeatherForecastEndpoint()
    {
        // Arrange
        using WebApplicationFactory<global::Program> factory =
            new ProjectAtmacaApiFactory();

        using HttpClient client =
            factory.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/WeatherForecast",
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
}