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

using ProjectAtmaca.Api.Security;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    RecordParticipationArrivalEndpointAuthorizationTests
{
    private const string TestAuthenticationScheme =
        "RecordArrivalEndpointTest";

    [Fact]
    public async Task Post_Should_ChallengeUnauthenticatedRequest()
    {
        Guid participationId =
            Guid.NewGuid();

        var request =
            new
            {
                JoinedAt =
                    new DateTimeOffset(
                        2026,
                        9,
                        10,
                        10,
                        30,
                        0,
                        TimeSpan.Zero)
            };

        using ProjectAtmacaApiFactory factory =
            new();

        using HttpClient client =
            factory.CreateClient();

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/" +
                $"{participationId:D}/record-arrival",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_Should_RecordArrivalThroughProductionHandler_WhenRequestIsAuthorized()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                10,
                10,
                30,
                0,
                TimeSpan.Zero);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                participation);

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient();

        var request =
            new
            {
                JoinedAt =
                    joinedAt
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "record-arrival",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        responseText
            .Should()
            .BeEmpty();

        participation.JoinedAt
            .Should()
            .Be(joinedAt);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.RecordArrival);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        repository.ObservedParticipationId
            .Should()
            .Be(
                participation.ParticipationId);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Post_Should_ReturnCanonicalNotFoundProblemDetails_WhenParticipationDoesNotExist()
    {
        ActorId actorId =
            ActorId.New();

        Guid missingParticipationId =
            Guid.NewGuid();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                10,
                10,
                30,
                0,
                TimeSpan.Zero);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                participation: null);

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient();

        var request =
            new
            {
                JoinedAt =
                    joinedAt
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/" +
                $"{missingParticipationId:D}/" +
                "record-arrival",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        using JsonDocument problem =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken));

        problem.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Not Found");

        problem.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be("The requested participation was not found.");

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("Participation.RecordArrival.NotFound");

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.RecordArrival);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        repository.ObservedParticipationId
            .Should()
            .Be(
                ParticipationId.From(
                    missingParticipationId));

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Post_Should_ReturnCanonicalProblemDetailsWithoutPersistence_WhenPermissionIsDenied()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                10,
                10,
                30,
                0,
                TimeSpan.Zero);

        RecordingActorAuthorizationService
            authorizationService =
                new()
                {
                    ShouldDeny = true
                };

        TrackingParticipationRepository repository =
            new(
                participation);

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient();

        var request =
            new
            {
                JoinedAt =
                    joinedAt
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "record-arrival",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        using JsonDocument problem =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken));

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
                ActorAuthorizationErrors.Forbidden.Message);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                ActorAuthorizationErrors.Forbidden.Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.RecordArrival);

        repository.GetByIdCallCount
            .Should()
            .Be(0);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);

        participation.JoinedAt
            .Should()
            .BeNull();
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Post_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenParticipationIdIsInvalid(
        string participationId)
    {
        ActorId actorId =
            ActorId.New();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                10,
                10,
                30,
                0,
                TimeSpan.Zero);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                participation: null);

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient();

        var request =
            new
            {
                JoinedAt =
                    joinedAt
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/{participationId}/" +
                "record-arrival",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        using JsonDocument problem =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken));

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

        repository.GetByIdCallCount
            .Should()
            .Be(0);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Post_Should_ReturnCanonicalBadRequestWithoutCommit_WhenParticipationIsAbsent()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

        Result markAbsentResult =
            participation.MarkAbsent();

        markAbsentResult.IsSuccess
            .Should()
            .BeTrue();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                10,
                10,
                30,
                0,
                TimeSpan.Zero);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                participation);

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient();

        var request =
            new
            {
                JoinedAt =
                    joinedAt
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "record-arrival",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        using JsonDocument problem =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken));

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
                ParticipationErrors
                    .ArrivalCannotBeRecordedWhenAbsent.Message);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                ParticipationErrors
                    .ArrivalCannotBeRecordedWhenAbsent.Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.RecordArrival);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(ParticipationStatus.Absent);

        participation.JoinedAt
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task Post_Should_ReturnCanonicalConflictWithoutCommit_WhenRecordedArrivalRequiresCorrection()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

        DateTimeOffset recordedJoinedAt =
            new(
                2026,
                9,
                10,
                10,
                30,
                0,
                TimeSpan.Zero);

        DateTimeOffset requestedJoinedAt =
            recordedJoinedAt.AddMinutes(15);

        Result initialArrivalResult =
            participation.RecordArrival(
                recordedJoinedAt);

        initialArrivalResult.IsSuccess
            .Should()
            .BeTrue();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                participation);

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient();

        var request =
            new
            {
                JoinedAt =
                    requestedJoinedAt
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "record-arrival",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        response.Content.Headers.ContentType
            ?.MediaType
            .Should()
            .Be("application/problem+json");

        using JsonDocument problem =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken));

        problem.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Conflict");

        problem.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                ParticipationErrors
                    .ArrivalCorrectionRequired.Message);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                ParticipationErrors
                    .ArrivalCorrectionRequired.Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.RecordArrival);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);

        participation.JoinedAt
            .Should()
            .Be(recordedJoinedAt);
    }

    [Fact]
    public async Task Post_Should_ReturnNoContentWithoutChangingArrival_WhenSameTimestampIsRepeated()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                10,
                9,
                15,
                0,
                TimeSpan.FromHours(3));

        Result initialArrivalResult =
            participation.RecordArrival(
                joinedAt);

        initialArrivalResult.IsSuccess
            .Should()
            .BeTrue();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                participation);

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateFactory(
                rootFactory,
                actorId,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient();

        string requestJson =
            $"{{\"joinedAt\":\"{joinedAt:O}\"}}";

        using StringContent content =
            new(
                requestJson,
                System.Text.Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "record-arrival",
                content,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        responseText
            .Should()
            .BeEmpty();

        participation.JoinedAt
            .Should()
            .Be(joinedAt);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.RecordArrival);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        repository.ObservedParticipationId
            .Should()
            .Be(
                participation.ParticipationId);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(1);
    }
    private static WebApplicationFactory<global::Program>
        CreateFactory(
            ProjectAtmacaApiFactory rootFactory,
            ActorId actorId,
            RecordingActorAuthorizationService
                authorizationService,
            TrackingParticipationRepository repository,
            TrackingUnitOfWork unitOfWork)
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
                            IParticipationRepository>();

                        services.AddScoped<
                            IParticipationRepository>(
                            _ =>
                                repository);

                        services.RemoveAll<
                            IUnitOfWork>();

                        services.AddScoped<
                            IUnitOfWork>(
                            _ =>
                                unitOfWork);

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
                                RecordArrivalEndpointTestAuthenticationHandler>(
                                TestAuthenticationScheme,
                                _ =>
                                {
                                });
                    });
            });
    }

    private static Participation CreateParticipation()
    {
        Result<Participation> result =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.From(
                        Guid.NewGuid())),
                AtmacaCardId.From(
                    Guid.NewGuid()));

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error?.Message);
        }

        return result.Value!;
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
        public bool ShouldDeny { get; init; }

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
                ShouldDeny
                    ? Result.Failure(
                        ActorAuthorizationErrors.Forbidden)
                    : Result.Success());
        }
    }

    private sealed class TrackingParticipationRepository
        : IParticipationRepository
    {
        private readonly Participation?
            _participation;

        public TrackingParticipationRepository(
            Participation? participation)
        {
            _participation =
                participation;
        }

        public int GetByIdCallCount { get; private set; }

        public ParticipationId? ObservedParticipationId
        {
            get;
            private set;
        }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            ObservedParticipationId =
                id;

            return Task.FromResult<Participation?>(
                _participation);
        }

        public Task<bool> ExistsAsync(
            AtmacaCardId atmacaCardId,
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                false);
        }

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingUnitOfWork
        : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.FromResult(1);
        }
    }
}

public sealed class RecordArrivalEndpointTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RecordArrivalEndpointTestAuthenticationHandler(
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
                        "record-arrival-endpoint-subject"),
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