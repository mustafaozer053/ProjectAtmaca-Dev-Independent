using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectAtmaca.Api.Participations.GetById;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

using Xunit;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    GetParticipationByIdEndpointAuthorizationTests
{
    private const string TestAuthenticationScheme =
        "GetParticipationByIdEndpoint";

    [Fact]
    public async Task Get_Should_ChallengeUnauthenticatedRequest()
    {
        using ProjectAtmacaApiFactory factory =
            new();

        using HttpClient client =
            factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(
                $"/api/participations/{Guid.NewGuid():D}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Should_ReturnTransportResponseThroughProductionHandler_WhenRequestIsAuthorized()
    {
        ActorId actorId =
            ActorId.New();

        Guid participationId =
            Guid.NewGuid();

        Guid atmacaCardId =
            Guid.NewGuid();

        Guid activityId =
            Guid.NewGuid();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                9,
                10,
                30,
                0,
                TimeSpan.Zero);

        ParticipationDetails details =
            new(
                participationId,
                atmacaCardId,
                ActivityTypeCode.TrainingCode,
                activityId,
                ParticipationStatus.Present,
                "LATE",
                joinedAt,
                null,
                "Arrived late.");

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                details);

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                reader);

        using HttpClient client =
            factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(
                $"/api/participations/{participationId:D}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        GetParticipationByIdResponse? responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    GetParticipationByIdResponse>(
                    TestContext.Current.CancellationToken);

        responseBody
            .Should()
            .Be(
                new GetParticipationByIdResponse(
                    participationId,
                    atmacaCardId,
                    ActivityTypeCode.TrainingCode,
                    activityId,
                    "PRESENT",
                    "LATE",
                    joinedAt,
                    null,
                    "Arrived late."));

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.GetById);

        reader.GetByIdCallCount
            .Should()
            .Be(1);

        reader.ObservedParticipationId
            .Should()
            .Be(participationId);
    }

    [Fact]
    public async Task Get_Should_ReturnCanonicalNotFoundProblemDetails_WhenParticipationDoesNotExist()
    {
        ActorId actorId =
            ActorId.New();

        Guid missingParticipationId =
            Guid.NewGuid();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                null);

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                reader);

        using HttpClient client =
            factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(
                $"/api/participations/{missingParticipationId:D}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument problem =
            JsonDocument.Parse(
                responseText);

        problem.RootElement
            .GetProperty("status")
            .GetInt32()
            .Should()
            .Be(404);

        problem.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Not Found");

        problem.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                GetParticipationByIdErrors
                    .NotFound
                    .Message);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                GetParticipationByIdErrors
                    .NotFound
                    .Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.GetById);

        reader.GetByIdCallCount
            .Should()
            .Be(1);

        reader.ObservedParticipationId
            .Should()
            .Be(missingParticipationId);
    }
    [Fact]
    public async Task Get_Should_ReturnCanonicalForbiddenProblemDetailsWithoutReaderAccess_WhenPermissionIsDenied()
    {
        ActorId actorId =
            ActorId.New();

        Guid participationId =
            Guid.NewGuid();

        RecordingActorAuthorizationService
            authorizationService =
                new(
                    shouldDeny: true);

        TrackingParticipationReader reader =
            new(
                null);

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                reader);

        using HttpClient client =
            factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(
                $"/api/participations/{participationId:D}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument problem =
            JsonDocument.Parse(
                responseText);

        problem.RootElement
            .GetProperty("status")
            .GetInt32()
            .Should()
            .Be(403);

        problem.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Forbidden");

        problem.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                ActorAuthorizationErrors
                    .Forbidden
                    .Message);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                ActorAuthorizationErrors
                    .Forbidden
                    .Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.GetById);

        reader.GetByIdCallCount
            .Should()
            .Be(0);
    }
    [Fact]
    public async Task Get_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenParticipationIdIsMalformed()
    {
        ActorId actorId =
            ActorId.New();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                null);

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                reader);

        using HttpClient client =
            factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(
                "/api/participations/not-a-guid",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument problem =
            JsonDocument.Parse(
                responseText);

        problem.RootElement
            .GetProperty("status")
            .GetInt32()
            .Should()
            .Be(400);

        problem.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Bad Request");

        problem.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                "Participation id must be a non-empty GUID in D format.");

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("Participation.Id.Invalid");

        authorizationService.CallCount
            .Should()
            .Be(0);

        reader.GetByIdCallCount
            .Should()
            .Be(0);
    }
    [Fact]
    public async Task Get_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenParticipationIdIsEmpty()
    {
        ActorId actorId =
            ActorId.New();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                null);

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                reader);

        using HttpClient client =
            factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(
                $"/api/participations/{Guid.Empty:D}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument problem =
            JsonDocument.Parse(
                responseText);

        problem.RootElement
            .GetProperty("status")
            .GetInt32()
            .Should()
            .Be(400);

        problem.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Bad Request");

        problem.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                "Participation id must be a non-empty GUID in D format.");

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("Participation.Id.Invalid");

        authorizationService.CallCount
            .Should()
            .Be(0);

        reader.GetByIdCallCount
            .Should()
            .Be(0);
    }
    private static WebApplicationFactory<global::Program>
        CreateFactory(
            ProjectAtmacaApiFactory rootFactory,
            ActorId actorId,
            RecordingActorAuthorizationService
                authorizationService,
            TrackingParticipationReader reader)
    {
        return rootFactory.WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureServices(
                    services =>
                    {
                        services.RemoveAll<
                            IActorIdentityResolver>();

                        services.AddScoped<
                            IActorIdentityResolver>(
                            _ =>
                                new FixedActorIdentityResolver(
                                    actorId));

                        services.RemoveAll<
                            IActorAuthorizationService>();

                        services.AddScoped<
                            IActorAuthorizationService>(
                            _ =>
                                authorizationService);

                        services.RemoveAll<
                            IParticipationReader>();

                        services.AddScoped<
                            IParticipationReader>(
                            _ =>
                                reader);

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
                                GetParticipationByIdTestAuthenticationHandler>(
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
        private readonly ActorId _actorId;

        public FixedActorIdentityResolver(
            ActorId actorId)
        {
            _actorId =
                actorId;
        }

        public Task<Result<ActorId>> ResolveAsync(
            ExternalIdentity externalIdentity,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Result<ActorId>.Success(
                    _actorId));
        }
    }

    private sealed class RecordingActorAuthorizationService
        : IActorAuthorizationService
    {
        private readonly bool _shouldDeny;

        public RecordingActorAuthorizationService(
            bool shouldDeny = false)
        {
            _shouldDeny =
                shouldDeny;
        }

        public int CallCount { get; private set; }

        public Permission? ObservedPermission
        {
            get;
            private set;
        }

        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ObservedPermission =
                permission;

            return Task.FromResult(
                _shouldDeny
                    ? Result.Failure(
                        ActorAuthorizationErrors
                            .Forbidden)
                    : Result.Success());
        }
    }

    private sealed class TrackingParticipationReader
        : IParticipationReader
    {
        private readonly ParticipationDetails? _details;

        public TrackingParticipationReader(
            ParticipationDetails? details)
        {
            _details =
                details;
        }

        public int GetByIdCallCount { get; private set; }

        public Guid ObservedParticipationId
        {
            get;
            private set;
        }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            ObservedParticipationId =
                participationId;

            return Task.FromResult<ParticipationDetails?>(
                _details);
        }

        public Task<IReadOnlyList<ParticipationListItem>>
            ListByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ParticipationActivitySummary>
            GetSummaryByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ParticipationHistoryPage>
            ListHistoryByAtmacaCardAsync(
                AtmacaCardId atmacaCardId,
                int pageSize,
                ParticipationHistoryCursor? cursor,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}

public sealed class
    GetParticipationByIdTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public GetParticipationByIdTestAuthenticationHandler(
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
                        "get-participation-by-id-subject")
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