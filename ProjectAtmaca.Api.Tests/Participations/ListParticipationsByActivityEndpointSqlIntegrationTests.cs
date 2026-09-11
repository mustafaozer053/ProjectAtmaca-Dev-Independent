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
using ProjectAtmaca.Api.Participations.ListByActivity;
using ProjectAtmaca.Api.Participations.MarkPresent;
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
    ListParticipationsByActivityEndpointSqlIntegrationTests
{
    private const string TestAuthenticationScheme =
        "ListParticipationsByActivitySqlIntegration";

    internal const string ExternalSubject =
        "list-participations-by-activity-sql-subject";

    [Fact]
    public async Task
        Get_Should_FilterProjectAndOrderThroughCanonicalProductionSqlPipeline()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;

        string databaseName =
            $"ProjectAtmaca_Api_ListByActivity_{Guid.NewGuid():N}";

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

        Guid presentAtmacaCardId =
            Guid.NewGuid();

        Guid notRecordedAtmacaCardId =
            Guid.NewGuid();

        Guid excludedAtmacaCardId =
            Guid.NewGuid();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                11,
                9,
                15,
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

                Permission[] requiredPermissions =
                [
                    Permissions.Participations.Create,
                    Permissions.Participations.MarkPresent,
                    Permissions.Participations.RecordArrival,
                    Permissions.Participations.RecordDeparture,
                    Permissions.Participations.ListByActivity
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

            Guid presentParticipationId =
                await CreateParticipationAsync(
                    client,
                    targetActivityId,
                    presentAtmacaCardId,
                    cancellationToken);

            Guid notRecordedParticipationId =
                await CreateParticipationAsync(
                    client,
                    targetActivityId,
                    notRecordedAtmacaCardId,
                    cancellationToken);

            Guid excludedParticipationId =
                await CreateParticipationAsync(
                    client,
                    excludedActivityId,
                    excludedAtmacaCardId,
                    cancellationToken);

            await MarkPresentAsync(
                client,
                presentParticipationId,
                ParticipationCondition.Late.Code,
                cancellationToken);

            await RecordArrivalAsync(
                client,
                presentParticipationId,
                joinedAt,
                cancellationToken);

            await RecordDepartureAsync(
                client,
                presentParticipationId,
                leftAt,
                cancellationToken);

            using HttpResponseMessage listResponse =
                await client.GetAsync(
                    "/api/participations" +
                    "?activityTypeCode=TRAINING" +
                    $"&activityId={targetActivityId:D}",
                    cancellationToken);

            listResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            listResponse.Content.Headers.ContentType
                .Should()
                .NotBeNull();

            listResponse.Content.Headers.ContentType!
                .MediaType
                .Should()
                .Be("application/json");

            ParticipationListItemResponse[]?
                responseBody =
                    await listResponse.Content
                        .ReadFromJsonAsync<
                            ParticipationListItemResponse[]>(
                            cancellationToken);

            responseBody
                .Should()
                .NotBeNull();

            ParticipationListItemResponse[] items =
                responseBody!;

            items
                .Should()
                .HaveCount(2);

            Guid[] expectedOrder =
            [
                presentParticipationId,
                notRecordedParticipationId
            ];

            expectedOrder =
                expectedOrder
                    .OrderBy(
                        participationId =>
                            participationId)
                    .ToArray();

            items
                .Select(
                    item =>
                        item.Id)
                .Should()
                .Equal(
                    expectedOrder);

            items
                .Should()
                .NotContain(
                    item =>
                        item.Id ==
                        excludedParticipationId);

            ParticipationListItemResponse presentItem =
                items.Single(
                    item =>
                        item.Id ==
                        presentParticipationId);

            presentItem.AtmacaCardId
                .Should()
                .Be(presentAtmacaCardId);

            presentItem.Status
                .Should()
                .Be("PRESENT");

            presentItem.ConditionCode
                .Should()
                .Be(
                    ParticipationCondition.Late.Code);

            presentItem.JoinedAt
                .Should()
                .Be(joinedAt);

            presentItem.LeftAt
                .Should()
                .Be(leftAt);

            ParticipationListItemResponse
                notRecordedItem =
                    items.Single(
                        item =>
                            item.Id ==
                            notRecordedParticipationId);

            notRecordedItem.AtmacaCardId
                .Should()
                .Be(notRecordedAtmacaCardId);

            notRecordedItem.Status
                .Should()
                .Be("NOT_RECORDED");

            notRecordedItem.ConditionCode
                .Should()
                .BeNull();

            notRecordedItem.JoinedAt
                .Should()
                .BeNull();

            notRecordedItem.LeftAt
                .Should()
                .BeNull();

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

                int persistedParticipationCount =
                    await verificationContext.Participations
                        .AsNoTracking()
                        .CountAsync(
                            cancellationToken);

                persistedParticipationCount
                    .Should()
                    .Be(3);
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
        string conditionCode,
        CancellationToken cancellationToken)
    {
        MarkParticipationPresentRequest request =
            new(
                conditionCode);

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

    private static async Task RecordArrivalAsync(
        HttpClient client,
        Guid participationId,
        DateTimeOffset joinedAt,
        CancellationToken cancellationToken)
    {
        RecordParticipationArrivalRequest request =
            new(
                joinedAt);

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/{participationId:D}/" +
                "record-arrival",
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

    private static async Task RecordDepartureAsync(
        HttpClient client,
        Guid participationId,
        DateTimeOffset leftAt,
        CancellationToken cancellationToken)
    {
        RecordParticipationDepartureRequest request =
            new(
                leftAt);

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/{participationId:D}/" +
                "record-departure",
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
                                ListParticipationsByActivitySqlTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }
}

public sealed class
    ListParticipationsByActivitySqlTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public ListParticipationsByActivitySqlTestAuthenticationHandler(
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
                        ListParticipationsByActivityEndpointSqlIntegrationTests
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
