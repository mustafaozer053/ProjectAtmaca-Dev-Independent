using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectAtmaca.Api.Security;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Tests.Security;

public sealed class ActorAuthorizationProductionCompositionTests
{
    private const string TestAuthenticationScheme =
        "ActorAuthorizationTest";

    [Fact]
    public async Task ProductionHost_Should_AllowMappedActor_AndExposeCanonicalActorId()
    {
        // Arrange
        ActorId expectedActorId =
            ActorId.New();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                Result<ActorId>.Success(
                    expectedActorId));

        using HttpClient client =
            factory.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/__security/actor-authorization/actor",
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        string actorId =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        actorId
            .Should()
            .Be(
                expectedActorId.Value.ToString("D"));
    }

    [Fact]
    public async Task ProductionHost_Should_ForbidAuthenticatedIdentityWithoutActorMapping()
    {
        // Arrange
        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                Result<ActorId>.Failure(
                    ActorIdentityResolutionErrors.NotMapped));

        using HttpClient client =
            factory.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/__security/actor-authorization/access",
                TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);
    }

    private static WebApplicationFactory<global::Program>
        CreateFactory(
            ProjectAtmacaApiFactory rootFactory,
            Result<ActorId> resolution)
    {
        return rootFactory.WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureServices(
                    services =>
                    {
                        services
                            .AddControllers()
                            .AddApplicationPart(
                                typeof(
                                    ActorAuthorizationProbeController)
                                .Assembly);

                        services.RemoveAll<
                            IActorIdentityResolver>();

                        services.AddScoped<
                            IActorIdentityResolver>(
                            _ =>
                                new FixedActorIdentityResolver(
                                    resolution));

                        services
                            .AddAuthentication(
                                options =>
                                {
                                    options.DefaultAuthenticateScheme =
                                        TestAuthenticationScheme;

                                    options.DefaultChallengeScheme =
                                        TestAuthenticationScheme;

                                    options.DefaultForbidScheme =
                                        TestAuthenticationScheme;
                                })
                            .AddScheme<
                                AuthenticationSchemeOptions,
                                ActorAuthorizationTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }

    private sealed class FixedActorIdentityResolver
        : IActorIdentityResolver
    {
        private readonly Result<ActorId> _resolution;

        public FixedActorIdentityResolver(
            Result<ActorId> resolution)
        {
            _resolution =
                resolution;
        }

        public Task<Result<ActorId>> ResolveAsync(
            ExternalIdentity externalIdentity,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _resolution);
        }
    }
}

public sealed class ActorAuthorizationTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public ActorAuthorizationTestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(
            options,
            logger,
            encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        ClaimsIdentity identity =
            new(
                [
                    new Claim(
                        "iss",
                        ProjectAtmacaApiFactory
                            .AuthenticationAuthority),
                    new Claim(
                        "sub",
                        "actor-authorization-subject"),
                    new Claim(
                        ActorClaimTypes.ActorId,
                        ActorId.New()
                            .Value
                            .ToString("D"))
                ],
                Scheme.Name);

        ClaimsPrincipal principal =
            new(
                identity);

        AuthenticationTicket ticket =
            new(
                principal,
                Scheme.Name);

        return Task.FromResult(
            AuthenticateResult.Success(
                ticket));
    }
}

[ApiController]
[Route("__security/actor-authorization")]
public sealed class ActorAuthorizationProbeController
    : ControllerBase
{
    [HttpGet("access")]
    public IActionResult Access()
    {
        return Ok();
    }

    [HttpGet("actor")]
    public IActionResult Actor(
        [FromServices] ICurrentActor currentActor)
    {
        return Ok(
            currentActor.ActorId
                .Value
                .ToString("D"));
    }
}