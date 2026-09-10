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
    MarkParticipationPresentEndpointSqlIntegrationTests
{
    private const string TestAuthenticationScheme =
        "MarkParticipationPresentSqlIntegration";

    internal const string ExternalSubject =
        "mark-participation-present-sql-subject";

    [Fact]
    public async Task Post_Should_PersistThroughCanonicalProductionSecurityAndAuditPipeline()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;

        string databaseName =
            $"ProjectAtmaca_Api_MarkPresent_{Guid.NewGuid():N}";

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
                            Permissions.Participations.MarkPresent));

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

            Guid participationId =
                createResponseBody!.ParticipationId;

            MarkParticipationPresentRequest markPresentRequest =
                new(
                    "late");

            using HttpResponseMessage markPresentResponse =
                await client.PostAsJsonAsync(
                    $"/api/participations/" +
                    $"{participationId:D}/mark-present",
                    markPresentRequest,
                    cancellationToken);

            markPresentResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.NoContent);

            string responseText =
                await markPresentResponse.Content
                    .ReadAsStringAsync(
                        cancellationToken);

            responseText
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
                                participationId,
                            cancellationToken);

                persistedParticipation
                    .Should()
                    .NotBeNull();

                persistedParticipation!
                    .ParticipationId
                    .Value
                    .Should()
                    .Be(participationId);

                persistedParticipation
                    .Status
                    .Should()
                    .Be(
                        ParticipationStatus.Present);

                persistedParticipation
                    .Condition
                    .Should()
                    .Be(
                        ParticipationCondition.Late);

                persistedParticipation
                    .Condition!
                    .Code
                    .Should()
                    .Be(
                        ParticipationCondition.LateCode);

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
                                MarkParticipationPresentSqlTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }
}

public sealed class
    MarkParticipationPresentSqlTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MarkParticipationPresentSqlTestAuthenticationHandler(
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
                        MarkParticipationPresentEndpointSqlIntegrationTests
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