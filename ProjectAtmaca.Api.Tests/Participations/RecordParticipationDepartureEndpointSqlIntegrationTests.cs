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
using ProjectAtmaca.Api.Participations.RecordArrival;
using ProjectAtmaca.Api.Participations.RecordDeparture;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;

using Xunit;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    RecordParticipationDepartureEndpointSqlIntegrationTests
{
    private const string TestAuthenticationScheme =
        "RecordParticipationDepartureSqlIntegration";

    internal const string ExternalSubject =
        "record-participation-departure-sql-subject";

    [Fact]
    public async Task Post_Should_PersistThroughCanonicalProductionSecurityAndAuditPipeline()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;

        string databaseName =
            $"ProjectAtmaca_Api_RecordDeparture_{Guid.NewGuid():N}";

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

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                10,
                10,
                30,
                0,
                TimeSpan.FromHours(3));

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

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
                            Permissions.Participations.RecordArrival));

                setupContext
                    .Set<ActorPermissionGrant>()
                    .Add(
                        ActorPermissionGrant.Create(
                            expectedActorId,
                            Permissions.Participations.RecordDeparture));

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
                    activityIdValue,
                    atmacaCardIdValue);

            using HttpResponseMessage createResponse =
                await client.PostAsJsonAsync(
                    "/api/participations",
                    createRequest,
                    cancellationToken);

            createResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.Created);

            CreateParticipationResponse? createResponseBody =
                await createResponse.Content
                    .ReadFromJsonAsync<
                        CreateParticipationResponse>(
                        cancellationToken);

            createResponseBody
                .Should()
                .NotBeNull();

            Guid participationIdValue =
                createResponseBody!.ParticipationId;

            RecordParticipationArrivalRequest
                recordArrivalRequest =
                    new(
                        joinedAt);

            using HttpResponseMessage recordArrivalResponse =
                await client.PostAsJsonAsync(
                    $"/api/participations/" +
                    $"{participationIdValue:D}/" +
                    "record-arrival",
                    recordArrivalRequest,
                    cancellationToken);

            recordArrivalResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.NoContent);

            string responseText =
                await recordArrivalResponse.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            responseText
                .Should()
                .BeEmpty();

            RecordParticipationDepartureRequest
                recordDepartureRequest =
                    new(
                        leftAt);

            using HttpResponseMessage recordDepartureResponse =
                await client.PostAsJsonAsync(
                    $"/api/participations/" +
                    $"{participationIdValue:D}/" +
                    "record-departure",
                    recordDepartureRequest,
                    cancellationToken);

            recordDepartureResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.NoContent);

            string departureResponseText =
                await recordDepartureResponse.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            departureResponseText
                .Should()
                .BeEmpty();

            await using (
                AsyncServiceScope verificationScope =
                    factory.Services.CreateAsyncScope())
            {
                ProjectAtmacaDbContext verificationContext =
                    verificationScope.ServiceProvider
                        .GetRequiredService<
                            ProjectAtmacaDbContext>();

                verificationContext.Database
                    .GetDbConnection()
                    .Database
                    .Should()
                    .Be(databaseName);

                Participation? persistedParticipation =
                    await verificationContext.Participations
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            participation =>
                                participation.Id ==
                                participationIdValue,
                            cancellationToken);

                persistedParticipation
                    .Should()
                    .NotBeNull();

                persistedParticipation!
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.NotRecorded);

                persistedParticipation
                    .JoinedAt
                    .Should()
                    .Be(joinedAt);

                persistedParticipation
                    .LeftAt
                    .Should()
                    .Be(leftAt);

                persistedParticipation
                    .CreatedByActorId
                    .Should()
                    .Be(expectedActorId);

                persistedParticipation
                    .LastModifiedByActorId
                    .Should()
                    .Be(expectedActorId);
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
                                RecordParticipationDepartureSqlTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }
}

public sealed class
    RecordParticipationDepartureSqlTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RecordParticipationDepartureSqlTestAuthenticationHandler(
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
                        RecordParticipationDepartureEndpointSqlIntegrationTests
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