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
using ProjectAtmaca.Api.Participations.GetSummaryByActivity;
using ProjectAtmaca.Api.Participations.MarkPresent;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;

using Xunit;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    GetParticipationSummaryByActivityEndpointSqlIntegrationTests
{
    private const string TestAuthenticationScheme =
        "GetParticipationSummaryByActivitySqlIntegration";

    internal const string ExternalSubject =
        "get-participation-summary-by-activity-sql-subject";

    [Fact]
    public async Task
        Get_Should_AggregateTargetStatusesThroughCanonicalProductionSqlPipeline()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;

        string databaseName =
            $"ProjectAtmaca_Api_GetSummary_{Guid.NewGuid():N}";

        string connectionString =
            "Server=(localdb)\\mssqllocaldb;" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True";

        ActorId expectedActorId =
            ActorId.New();

        Guid targetActivityId =
            Guid.NewGuid();

        Guid excludedActivityId =
            Guid.NewGuid();

        Guid notRecordedAtmacaCardId =
            Guid.NewGuid();

        Guid firstPresentAtmacaCardId =
            Guid.NewGuid();

        Guid secondPresentAtmacaCardId =
            Guid.NewGuid();

        Guid absentAtmacaCardId =
            Guid.NewGuid();

        Guid excludedPresentAtmacaCardId =
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

                Permission[] requiredPermissions =
                [
                    Permissions.Participations.Create,
                    Permissions.Participations.MarkPresent,
                    Permissions.Participations
                        .GetSummaryByActivity
                ];

                foreach (
                    Permission permission in
                    requiredPermissions)
                {
                    setupContext
                        .Set<ActorPermissionGrant>()
                        .Add(
                            ActorPermissionGrant.Create(
                                expectedActorId,
                                permission));
                }

                await setupContext.SaveChangesAsync(
                    cancellationToken);
            }

            using HttpClient client =
                factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false
                    });

            Guid notRecordedParticipationId =
                await CreateParticipationAsync(
                    client,
                    targetActivityId,
                    notRecordedAtmacaCardId,
                    cancellationToken);

            Guid firstPresentParticipationId =
                await CreateParticipationAsync(
                    client,
                    targetActivityId,
                    firstPresentAtmacaCardId,
                    cancellationToken);

            Guid secondPresentParticipationId =
                await CreateParticipationAsync(
                    client,
                    targetActivityId,
                    secondPresentAtmacaCardId,
                    cancellationToken);

            Guid absentParticipationId =
                await CreateParticipationAsync(
                    client,
                    targetActivityId,
                    absentAtmacaCardId,
                    cancellationToken);

            Guid excludedPresentParticipationId =
                await CreateParticipationAsync(
                    client,
                    excludedActivityId,
                    excludedPresentAtmacaCardId,
                    cancellationToken);

            await MarkPresentAsync(
                client,
                firstPresentParticipationId,
                cancellationToken);

            await MarkPresentAsync(
                client,
                secondPresentParticipationId,
                cancellationToken);

            await MarkPresentAsync(
                client,
                excludedPresentParticipationId,
                cancellationToken);

            await using (
                AsyncServiceScope arrangementScope =
                    factory.Services.CreateAsyncScope())
            {
                ProjectAtmacaDbContext arrangementContext =
                    arrangementScope.ServiceProvider
                        .GetRequiredService<
                            ProjectAtmacaDbContext>();

                arrangementContext.Database
                    .GetDbConnection()
                    .Database
                    .Should()
                    .Be(databaseName);

                int affectedRows =
                    await arrangementContext.Participations
                        .Where(
                            participation =>
                                participation.Id ==
                                absentParticipationId)
                        .ExecuteUpdateAsync(
                            setters =>
                                setters.SetProperty(
                                    participation =>
                                        participation.Status,
                                    ParticipationStatus.Absent),
                            cancellationToken);

                affectedRows
                    .Should()
                    .Be(1);
            }

            using HttpResponseMessage summaryResponse =
                await client.GetAsync(
                    "/api/participations/summary" +
                    "?activityTypeCode=TRAINING" +
                    $"&activityId={targetActivityId:D}",
                    cancellationToken);

            summaryResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            summaryResponse.Content.Headers.ContentType
                .Should()
                .NotBeNull();

            summaryResponse.Content.Headers.ContentType!
                .MediaType
                .Should()
                .Be("application/json");

            ParticipationActivitySummaryResponse?
                responseBody =
                    await summaryResponse.Content
                        .ReadFromJsonAsync<
                            ParticipationActivitySummaryResponse>(
                            cancellationToken);

            responseBody
                .Should()
                .NotBeNull();

            responseBody!
                .Should()
                .Be(
                    new ParticipationActivitySummaryResponse(
                        Total: 4,
                        NotRecorded: 1,
                        Present: 2,
                        Absent: 1));

            (
                responseBody.NotRecorded +
                responseBody.Present +
                responseBody.Absent)
                .Should()
                .Be(responseBody.Total);

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

                Participation[] persistedParticipations =
                    await verificationContext.Participations
                        .AsNoTracking()
                        .OrderBy(
                            participation =>
                                participation.Id)
                        .ToArrayAsync(
                            cancellationToken);

                persistedParticipations
                    .Should()
                    .HaveCount(5);

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            notRecordedParticipationId)
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.NotRecorded);

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            firstPresentParticipationId)
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.Present);

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            secondPresentParticipationId)
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.Present);

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            absentParticipationId)
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.Absent);

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            excludedPresentParticipationId)
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.Present);

                foreach (
                    Participation persistedParticipation in
                    persistedParticipations)
                {
                    persistedParticipation
                        .CreatedByActorId
                        .Should()
                        .Be(expectedActorId);
                }

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            notRecordedParticipationId)
                    .LastModifiedByActorId
                    .Should()
                    .BeNull();

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            firstPresentParticipationId)
                    .LastModifiedByActorId
                    .Should()
                    .Be(expectedActorId);

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            secondPresentParticipationId)
                    .LastModifiedByActorId
                    .Should()
                    .Be(expectedActorId);

                persistedParticipations
                    .Single(
                        participation =>
                            participation.Id ==
                            excludedPresentParticipationId)
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

    private static async Task<Guid>
        CreateParticipationAsync(
            HttpClient client,
            Guid activityId,
            Guid atmacaCardId,
            CancellationToken cancellationToken)
    {
        CreateParticipationRequest request =
            new(
                ActivityTypeCode.TrainingCode,
                activityId,
                atmacaCardId);

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

        return responseBody!.ParticipationId;
    }

    private static async Task MarkPresentAsync(
        HttpClient client,
        Guid participationId,
        CancellationToken cancellationToken)
    {
        MarkParticipationPresentRequest request =
            new(
                ParticipationCondition.Late.Code);

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/{participationId:D}/" +
                "mark-present",
                request,
                cancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        string responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        responseBody
            .Should()
            .BeEmpty();
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
                                GetParticipationSummaryByActivitySqlTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }
}

public sealed class
    GetParticipationSummaryByActivitySqlTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public GetParticipationSummaryByActivitySqlTestAuthenticationHandler(
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
                        GetParticipationSummaryByActivityEndpointSqlIntegrationTests
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
