using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectAtmaca.Api.Participations.Create;
using ProjectAtmaca.Api.Participations.GetById;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;

using Xunit;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    GetParticipationByIdEndpointSqlIntegrationTests
{
    private const string TestAuthenticationScheme =
        "GetParticipationByIdSqlIntegration";

    internal const string ExternalSubject =
        "get-participation-by-id-sql-subject";

    [Fact]
    public async Task Get_Should_ResolveCreatedLocationThroughCanonicalProductionSecurityAndReaderPipeline()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;

        string databaseName =
            $"ProjectAtmaca_Api_GetById_{Guid.NewGuid():N}";

        string connectionString =
            "Server=(localdb)\\mssqllocaldb;" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True";

        ActorId expectedActorId =
            ActorId.New();

        Guid activityId =
            Guid.NewGuid();

        Guid atmacaCardId =
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

                setupContext.Database
                    .GetDbConnection()
                    .Database
                    .Should()
                    .Be(databaseName);

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

                setupContext
                    .Set<ActorPermissionGrant>()
                    .Add(
                        ActorPermissionGrant.Create(
                            expectedActorId,
                            Permissions.Participations.GetById));

                await setupContext.SaveChangesAsync(
                    cancellationToken);
            }

            using HttpClient client =
                factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false
                    });

            CreateParticipationRequest createRequest =
                new(
                    "TRAINING",
                    activityId,
                    atmacaCardId);

            using HttpResponseMessage createResponse =
                await client.PostAsJsonAsync(
                    "/api/participations",
                    createRequest,
                    cancellationToken);

            createResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.Created);

            CreateParticipationResponse? nullableCreatedParticipation =
                await createResponse.Content
                    .ReadFromJsonAsync<
                        CreateParticipationResponse>(
                        cancellationToken);

            nullableCreatedParticipation
                .Should()
                .NotBeNull();

            CreateParticipationResponse createdParticipation =
                nullableCreatedParticipation!;

            createResponse.Headers.Location
                .Should()
                .NotBeNull();

            Uri createdLocation =
                createResponse.Headers.Location!;

            createdLocation
                .OriginalString
                .Should()
                .Be(
                    "/api/participations/" +
                    createdParticipation
                        .ParticipationId
                        .ToString("D"));

            using HttpResponseMessage getResponse =
                await client.GetAsync(
                    createdLocation,
                    cancellationToken);

            getResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            getResponse.Content.Headers.ContentType
                ?.MediaType
                .Should()
                .Be("application/json");

            GetParticipationByIdResponse? nullableDetails =
                await getResponse.Content
                    .ReadFromJsonAsync<
                        GetParticipationByIdResponse>(
                        cancellationToken);

            nullableDetails
                .Should()
                .NotBeNull();

            GetParticipationByIdResponse details =
                nullableDetails!;

            details.ParticipationId
                .Should()
                .Be(
                    createdParticipation.ParticipationId);

            details.AtmacaCardId
                .Should()
                .Be(atmacaCardId);

            details.ActivityTypeCode
                .Should()
                .Be("TRAINING");

            details.ActivityId
                .Should()
                .Be(activityId);

            details.Status
                .Should()
                .Be("NOT_RECORDED");

            details.ConditionCode
                .Should()
                .BeNull();

            details.JoinedAt
                .Should()
                .BeNull();

            details.LeftAt
                .Should()
                .BeNull();

            details.Note
                .Should()
                .BeNull();
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

                builder.ConfigureServices(
                    services =>
                    {
                        services
                            .ReplaceProjectAtmacaDatabase(
                                connectionString);

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
                                GetParticipationByIdSqlTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }
}

public sealed class
    GetParticipationByIdSqlTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public GetParticipationByIdSqlTestAuthenticationHandler(
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
                        GetParticipationByIdEndpointSqlIntegrationTests
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
