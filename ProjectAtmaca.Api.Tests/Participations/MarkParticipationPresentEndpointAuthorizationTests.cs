using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

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
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class MarkParticipationPresentEndpointAuthorizationTests
{
    private const string TestAuthenticationScheme =
        "MarkPresentEndpointTest";

    [Fact]
    public async Task Post_Should_ChallengeUnauthenticatedRequest()
    {
        Guid participationId =
            Guid.NewGuid();

        using ProjectAtmacaApiFactory factory =
            new();

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        using StringContent content =
            new(
                "{}",
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participationId:D}/mark-present",
                content,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_Should_MarkParticipationPresentThroughProductionHandler_WhenRequestIsAuthorized()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

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

        using StringContent content =
            new(
                "{\"conditionCode\":\"late\"}",
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "mark-present",
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

        participation.Status
            .Should()
            .Be(ParticipationStatus.Present);

        participation.Condition
            .Should()
            .BeSameAs(
                ParticipationCondition.Late);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.MarkPresent);

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

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                null);

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

        using StringContent content =
            new(
                "{}",
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{missingParticipationId:D}/" +
                "mark-present",
                content,
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
                MarkParticipationPresentErrors
                    .NotFound
                    .Message);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                MarkParticipationPresentErrors
                    .NotFound
                    .Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.MarkPresent);

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
    public async Task Post_Should_ReturnCanonicalForbiddenProblemDetailsWithoutPersistence_WhenPermissionIsDenied()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

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

        using StringContent content =
            new(
                "{}",
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "mark-present",
                content,
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
                Permissions.Participations.MarkPresent);

        repository.GetByIdCallCount
            .Should()
            .Be(0);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        participation.Condition
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

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                null);

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

        using StringContent content =
            new(
                "{}",
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participationId}/mark-present",
                content,
                TestContext.Current.CancellationToken);

        await AssertCanonicalBadRequestAsync(
            response,
            "Participation.Id.Invalid",
            "Participation id must be a non-empty GUID in D format.");

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

    [Theory]
    [InlineData("", "")]
    [InlineData("unknown", "UNKNOWN")]
    public async Task Post_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenConditionCodeIsUnsupported(
        string conditionCode,
        string normalizedCode)
    {
        ActorId actorId =
            ActorId.New();

        Guid participationId =
            Guid.NewGuid();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new(
                null);

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
            JsonSerializer.Serialize(
                new
                {
                    conditionCode
                });

        using StringContent content =
            new(
                requestJson,
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participationId:D}/mark-present",
                content,
                TestContext.Current.CancellationToken);

        await AssertCanonicalBadRequestAsync(
            response,
            "Participation.Condition.Unsupported",
            $"Participation condition '{normalizedCode}' " +
            "is not supported.");

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
    public async Task Post_Should_ReturnCanonicalBadRequestWithoutCommit_WhenConditionIsInvalidForPresent()
    {
        ActorId actorId =
            ActorId.New();

        Participation participation =
            CreateParticipation();

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

        using StringContent content =
            new(
                "{\"conditionCode\":\"BTA\"}",
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "mark-present",
                content,
                TestContext.Current.CancellationToken);

        await AssertCanonicalBadRequestAsync(
            response,
            "Participation.Condition.InvalidForStatus",
            "Participation condition is not valid " +
            "for the specified status.");

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.MarkPresent);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        repository.ObservedParticipationId
            .Should()
            .Be(
                participation.ParticipationId);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        participation.Condition
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task Post_Should_ReturnCanonicalConflictWithoutCommit_WhenEstablishedClassificationRequiresCorrection()
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

        using StringContent content =
            new(
                "{\"conditionCode\":null}",
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage response =
            await client.PostAsync(
                $"/api/participations/" +
                $"{participation.ParticipationId.Value:D}/" +
                "mark-present",
                content,
                TestContext.Current.CancellationToken);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.MarkPresent);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        repository.ObservedParticipationId
            .Should()
            .Be(
                participation.ParticipationId);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Absent);

        participation.Condition
            .Should()
            .BeNull();

        await AssertCanonicalConflictAsync(
            response,
            "Participation.Classification.CorrectionRequired",
            "An established participation classification " +
            "cannot be changed through an ordinary " +
            "classification operation.");
    }

    private static async Task AssertCanonicalConflictAsync(
        HttpResponseMessage response,
        string expectedCode,
        string expectedDetail)
    {
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

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
            .Be(409);

        problem.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Conflict");

        problem.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be(expectedDetail);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(expectedCode);
    }
    private static async Task AssertCanonicalBadRequestAsync(
        HttpResponseMessage response,
        string expectedCode,
        string expectedDetail)
    {
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
            .Be(expectedDetail);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(expectedCode);
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
                                MarkPresentEndpointTestAuthenticationHandler>(
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

        public CancellationToken
            ObservedCancellationToken
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

            ObservedCancellationToken =
                cancellationToken;

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

public sealed class MarkPresentEndpointTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MarkPresentEndpointTestAuthenticationHandler(
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
                        "mark-present-endpoint-subject"),
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
