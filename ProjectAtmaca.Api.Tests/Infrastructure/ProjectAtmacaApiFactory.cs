using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProjectAtmaca.Api.Tests.Infrastructure;

public sealed class ProjectAtmacaApiFactory
    : WebApplicationFactory<global::Program>
{
    public const string AuthenticationAuthority =
        "https://identity.projectatmaca.test";

    public const string AuthenticationAudience =
        "project-atmaca-api";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                Dictionary<string, string?>
                    authenticationConfiguration =
                        new()
                        {
                            ["Authentication:Authority"] =
                                AuthenticationAuthority,
                            ["Authentication:Audience"] =
                                AuthenticationAudience
                        };

                configuration.AddInMemoryCollection(
                    authenticationConfiguration);
            });
    }
}