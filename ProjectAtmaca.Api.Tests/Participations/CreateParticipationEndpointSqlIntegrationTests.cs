using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectAtmaca.Api.Participations.Create;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;

using Xunit;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    CreateParticipationEndpointSqlIntegrationTests
{
    private const string TestAuthenticationScheme =
        "CreateParticipationSqlIntegration";

    internal const string ExternalSubject =
        "create-participation-sql-subject";

    [Fact]
    public async Task Post_Should_PersistThroughCanonicalProductionSecurityAndAuditPipeline()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;

        string databaseName =
            $"ProjectAtmaca_Api_Create_{Guid.NewGuid():N}";

        string connectionString =
            "Server=(localdb)\\mssqllocaldb;" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True";

        ActorId expectedActorId =
            ActorId.New();

        Guid activityIdValue =
            Guid.NewGuid();

        Guid atmacaCardIdValue =
            Guid.NewGuid();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                connectionString);

        try
        {
            await using (
                AsyncServiceScope setupScope =
                    factory.Services.CreateAsyncScope())
            {
                ProjectAtmacaDbContext setupContext =
                    setupScope.ServiceProvider
                        .GetRequiredService<
                            ProjectAtmacaDbContext>();

                await setupContext.Database
                    .EnsureDeletedAsync(
                        cancellationToken);

                await setupContext.Database
                    .MigrateAsync(
                        cancellationToken);

                ExternalIdentity externalIdentity =
                    ExternalIdentity.Create(
                        ProjectAtmacaApiFactory
                            .AuthenticationAuthority,
                        ExternalSubject);

                setupContext
                    .Set<ActorIdentityMapping>()
                    .Add(
                        ActorIdentityMapping.Create(
                            externalIdentity,
                            expectedActorId));

                setupContext
                    .Set<ActorPermissionGrant>()
                    .Add(
                        ActorPermissionGrant.Create(
                            expectedActorId,
                            Permissions.Participations.Create));

                await setupContext.SaveChangesAsync(
                    cancellationToken);
            }

            using HttpClient client =
                factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false
                    });

            CreateParticipationRequest request =
                new(
                    "TRAINING",
                    activityIdValue,
                    atmacaCardIdValue);

            using HttpResponseMessage response =
                await client.PostAsJsonAsync(
                    "/api/participations",
                    request,
                    cancellationToken);

            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Created);

            CreateParticipationResponse? responseBody =
                await response.Content
                    .ReadFromJsonAsync<
                        CreateParticipationResponse>(
                        cancellationToken);

            responseBody
                .Should()
                .NotBeNull();

            response.Headers.Location
                .Should()
                .NotBeNull();

            response.Headers.Location!
                .OriginalString
                .Should()
                .Be(
                    "/api/participations/" +
                    responseBody!.ParticipationId
                        .ToString("D"));

            await using (
                AsyncServiceScope verificationScope =
                    factory.Services.CreateAsyncScope())
            {
                ProjectAtmacaDbContext verificationContext =
                    verificationScope.ServiceProvider
                        .GetRequiredService<
                            ProjectAtmacaDbContext>();

                Participation? persistedParticipation =
                    await verificationContext.Participations
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            participation =>
                                participation.Id ==
                                responseBody.ParticipationId,
                            cancellationToken);

                persistedParticipation
                    .Should()
                    .NotBeNull();

                persistedParticipation!
                    .ActivityReference
                    .ActivityType
                    .Value
                    .Should()
                    .Be("TRAINING");

                persistedParticipation
                    .ActivityReference
                    .ActivityId
                    .Should()
                    .Be(activityIdValue);

                persistedParticipation
                    .AtmacaCardId
                    .Should()
                    .Be(
                        AtmacaCardId.From(
                            atmacaCardIdValue));

                persistedParticipation
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.NotRecorded);

                persistedParticipation
                    .CreatedByActorId
                    .Should()
                    .Be(expectedActorId);

                persistedParticipation
                    .LastModifiedByActorId
                    .Should()
                    .BeNull();
            }
        }
        finally
        {
            await using AsyncServiceScope cleanupScope =
                factory.Services.CreateAsyncScope();

            ProjectAtmacaDbContext cleanupContext =
                cleanupScope.ServiceProvider
                    .GetRequiredService<
                        ProjectAtmacaDbContext>();

            await cleanupContext.Database
                .EnsureDeletedAsync(
                    CancellationToken.None);
        }
    }

    private static WebApplicationFactory<global::Program>
        CreateFactory(
            ProjectAtmacaApiFactory rootFactory,
            string connectionString)
    {
        return rootFactory.WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureAppConfiguration(
                    (_, configuration) =>
                    {
                        Dictionary<string, string?>
                            configurationValues =
                                new()
                                {
                                    [
                                        "ConnectionStrings:" +
                                        "ProjectAtmacaDatabase"
                                    ] =
                                        connectionString
                                };

                        configuration.AddInMemoryCollection(
                            configurationValues);
                    });

                builder.ConfigureServices(
                    services =>
                    {
                        services
                            .AddAuthentication(
                                options =>
                                {
                                    options
                                        .DefaultAuthenticateScheme =
                                            TestAuthenticationScheme;

                                    options
                                        .DefaultChallengeScheme =
                                            TestAuthenticationScheme;

                                    options
                                        .DefaultForbidScheme =
                                            TestAuthenticationScheme;
                                })
                            .AddScheme<
                                AuthenticationSchemeOptions,
                                CreateParticipationSqlTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }
}

public sealed class
    CreateParticipationSqlTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public CreateParticipationSqlTestAuthenticationHandler(
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
                        CreateParticipationEndpointSqlIntegrationTests
                            .ExternalSubject)
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