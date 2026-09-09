using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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

using ProjectAtmaca.Api.Participations.Create;

using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    CreateParticipationEndpointAuthorizationTests
{
    private const string TestAuthenticationScheme =
        "CreateParticipationEndpointTest";

    [Fact]
    public async Task
        Post_Should_ChallengeUnauthenticatedRequest()
    {
        using ProjectAtmacaApiFactory factory =
            new();

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        var request =
            new
            {
                ActivityTypeCode = "TRAINING",
                ActivityId = Guid.NewGuid(),
                AtmacaCardId = Guid.NewGuid()
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/participations",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        Post_Should_CreateParticipationThroughProductionHandler_WhenRequestIsAuthorized()
    {
        Guid activityId =
            Guid.NewGuid();

        Guid atmacaCardId =
            Guid.NewGuid();

        ConfigurableActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new();

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateAuthorizedFactory(
                rootFactory,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        var request =
            new
            {
                ActivityTypeCode = "TRAINING",
                ActivityId = activityId,
                AtmacaCardId = atmacaCardId
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/participations",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        CreateParticipationResponse? responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    CreateParticipationResponse>(
                    TestContext.Current.CancellationToken);

        responseBody
            .Should()
            .NotBeNull();

        responseBody!.ParticipationId
            .Should()
            .NotBe(Guid.Empty);

        response.Headers.Location
            .Should()
            .NotBeNull();

        response.Headers.Location!
            .OriginalString
            .Should()
            .Be(
                $"/api/participations/" +
                $"{responseBody.ParticipationId:D}");

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.RequestedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.Create);

        repository.ExistsCallCount
            .Should()
            .Be(1);

        repository.AddCallCount
            .Should()
            .Be(1);

        repository.AddedParticipation
            .Should()
            .NotBeNull();

        repository.AddedParticipation!
            .ParticipationId
            .Value
            .Should()
            .Be(
                responseBody.ParticipationId);

        repository.AddedParticipation
            .AtmacaCardId
            .Value
            .Should()
            .Be(
                atmacaCardId);

        repository.AddedParticipation
            .ActivityReference
            .ActivityId
            .Should()
            .Be(
                activityId);

        unitOfWork.SaveCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        Post_Should_ReturnCanonicalProblemDetailsWithoutPersistence_WhenPermissionIsDenied()
    {
        ConfigurableActorAuthorizationService
            authorizationService =
                new()
                {
                    AuthorizationResult =
                        Result.Failure(
                            ActorAuthorizationErrors
                                .Forbidden)
                };

        TrackingParticipationRepository repository =
            new();

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateAuthorizedFactory(
                rootFactory,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        var request =
            new
            {
                ActivityTypeCode = "TRAINING",
                ActivityId = Guid.NewGuid(),
                AtmacaCardId = Guid.NewGuid()
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/participations",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!
            .MediaType
            .Should()
            .Be("application/problem+json");

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument document =
            JsonDocument.Parse(
                responseText);

        JsonElement problem =
            document.RootElement;

        problem.GetProperty("status")
            .GetInt32()
            .Should()
            .Be((int)HttpStatusCode.Forbidden);

        problem.GetProperty("title")
            .GetString()
            .Should()
            .Be("Forbidden");

        problem.GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                ActorAuthorizationErrors
                    .Forbidden
                    .Message);

        problem.GetProperty("code")
            .GetString()
            .Should()
            .Be(
                ActorAuthorizationErrors
                    .Forbidden
                    .Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.RequestedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.Create);

        repository.ExistsCallCount
            .Should()
            .Be(0);

        repository.AddCallCount
            .Should()
            .Be(0);

        repository.AddedParticipation
            .Should()
            .BeNull();

        unitOfWork.SaveCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task
        Post_Should_ReturnCanonicalConflictWithoutAddingOrCommitting_WhenParticipationAlreadyExists()
    {
        ConfigurableActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new()
            {
                ExistsResult =
                    true
            };

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateAuthorizedFactory(
                rootFactory,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        var request =
            new
            {
                ActivityTypeCode = "TRAINING",
                ActivityId = Guid.NewGuid(),
                AtmacaCardId = Guid.NewGuid()
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/participations",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!
            .MediaType
            .Should()
            .Be("application/problem+json");

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument document =
            JsonDocument.Parse(
                responseText);

        JsonElement problem =
            document.RootElement;

        problem.GetProperty("status")
            .GetInt32()
            .Should()
            .Be((int)HttpStatusCode.Conflict);

        problem.GetProperty("title")
            .GetString()
            .Should()
            .Be("Conflict");

        problem.GetProperty("detail")
            .GetString()
            .Should()
            .Be(
                CreateParticipationErrors
                    .AlreadyExists
                    .Message);

        problem.GetProperty("code")
            .GetString()
            .Should()
            .Be(
                CreateParticipationErrors
                    .AlreadyExists
                    .Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.RequestedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.Create);

        repository.ExistsCallCount
            .Should()
            .Be(1);

        repository.AddCallCount
            .Should()
            .Be(0);

        repository.AddedParticipation
            .Should()
            .BeNull();

        unitOfWork.SaveCallCount
            .Should()
            .Be(0);
    }

    [Theory]
    [MemberData(nameof(InvalidCreateRequests))]
    public async Task
        Post_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenTransportInputIsInvalid(
            string activityTypeCode,
            Guid activityId,
            Guid atmacaCardId,
            string expectedCode,
            string expectedDetail)
    {
        ConfigurableActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new();

        TrackingUnitOfWork unitOfWork =
            new();

        using ProjectAtmacaApiFactory rootFactory =
            new();

        using WebApplicationFactory<global::Program> factory =
            CreateAuthorizedFactory(
                rootFactory,
                authorizationService,
                repository,
                unitOfWork);

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        var request =
            new
            {
                ActivityTypeCode = activityTypeCode,
                ActivityId = activityId,
                AtmacaCardId = atmacaCardId
            };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/participations",
                request,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!
            .MediaType
            .Should()
            .Be("application/problem+json");

        string responseText =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument document =
            JsonDocument.Parse(
                responseText);

        JsonElement problem =
            document.RootElement;

        problem.GetProperty("status")
            .GetInt32()
            .Should()
            .Be((int)HttpStatusCode.BadRequest);

        problem.GetProperty("title")
            .GetString()
            .Should()
            .Be("Bad Request");

        problem.GetProperty("detail")
            .GetString()
            .Should()
            .Be(expectedDetail);

        problem.GetProperty("code")
            .GetString()
            .Should()
            .Be(expectedCode);

        authorizationService.CallCount
            .Should()
            .Be(0);

        authorizationService.RequestedPermission
            .Should()
            .BeNull();

        repository.ExistsCallCount
            .Should()
            .Be(0);

        repository.AddCallCount
            .Should()
            .Be(0);

        repository.AddedParticipation
            .Should()
            .BeNull();

        unitOfWork.SaveCallCount
            .Should()
            .Be(0);
    }

    public static IEnumerable<object[]>
        InvalidCreateRequests()
    {
        Result<ActivityTypeCode> missingActivityType =
            ActivityTypeCode.Create(
                string.Empty);

        Result<ActivityTypeCode> unsupportedActivityType =
            ActivityTypeCode.Create(
                "MATCH");

        yield return
            new object[]
            {
                string.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                missingActivityType.Error!.Code,
                missingActivityType.Error.Message
            };

        yield return
            new object[]
            {
                "MATCH",
                Guid.NewGuid(),
                Guid.NewGuid(),
                unsupportedActivityType.Error!.Code,
                unsupportedActivityType.Error.Message
            };

        yield return
            new object[]
            {
                ActivityTypeCode.TrainingCode,
                Guid.Empty,
                Guid.NewGuid(),
                "Participation.ActivityId.Required",
                "Activity id is required."
            };

        yield return
            new object[]
            {
                ActivityTypeCode.TrainingCode,
                Guid.NewGuid(),
                Guid.Empty,
                ParticipationErrors.AtmacaCardRequired.Code,
                ParticipationErrors.AtmacaCardRequired.Message
            };
    }

    private static WebApplicationFactory<global::Program>
        CreateAuthorizedFactory(
            ProjectAtmacaApiFactory rootFactory,
            ConfigurableActorAuthorizationService
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

                        services.AddSingleton<
                            IActorIdentityResolver>(
                            new FixedActorIdentityResolver(
                                ActorId.New()));

                        services.RemoveAll<
                            IActorAuthorizationService>();

                        services.AddSingleton<
                            IActorAuthorizationService>(
                            authorizationService);

                        services.RemoveAll<
                            IParticipationRepository>();

                        services.AddSingleton<
                            IParticipationRepository>(
                            repository);

                        services.RemoveAll<
                            IUnitOfWork>();

                        services.AddSingleton<
                            IUnitOfWork>(
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
                                CreateParticipationEndpointAuthenticationHandler>(
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

    private sealed class
        ConfigurableActorAuthorizationService
        : IActorAuthorizationService
    {
        public Result AuthorizationResult { get; init; } =
            Result.Success();

        public int CallCount { get; private set; }

        public Permission? RequestedPermission
        {
            get;
            private set;
        }

        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            RequestedPermission =
                permission;

            return Task.FromResult(
                AuthorizationResult);
        }
    }

    private sealed class
        TrackingParticipationRepository
        : IParticipationRepository
    {
        public bool ExistsResult { get; init; }

        public int ExistsCallCount { get; private set; }

        public int AddCallCount { get; private set; }

        public Participation? AddedParticipation
        {
            get;
            private set;
        }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Participation?>(
                null);
        }

        public Task<bool> ExistsAsync(
            AtmacaCardId atmacaCardId,
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
        {
            ExistsCallCount++;

            return Task.FromResult(
                ExistsResult);
        }

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            AddedParticipation =
                participation;

            return Task.CompletedTask;
        }
    }

    private sealed class TrackingUnitOfWork
        : IUnitOfWork
    {
        public int SaveCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCallCount++;

            return Task.FromResult(
                1);
        }
    }
}


public sealed class
    CreateParticipationEndpointAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public CreateParticipationEndpointAuthenticationHandler(
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
                        "create-participation-endpoint-subject")
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
