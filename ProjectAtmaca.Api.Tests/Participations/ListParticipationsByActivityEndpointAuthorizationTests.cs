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

using ProjectAtmaca.Api.Participations.ListByActivity;
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
    ListParticipationsByActivityEndpointAuthorizationTests
{
    private const string TestAuthenticationScheme =
        "ListParticipationsByActivityEndpoint";

    [Fact]
    public async Task
        Get_Should_ChallengeUnauthenticatedRequest()
    {
        using ProjectAtmacaApiFactory factory =
            new();

        using HttpClient client =
            factory.CreateClient();

        Guid activityId =
            Guid.NewGuid();

        string requestUri =
            $"/api/participations" +
            $"?activityTypeCode=" +
            $"{ActivityTypeCode.TrainingCode}" +
            $"&activityId={activityId:D}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        Get_Should_ReturnTransportResponseThroughProductionHandler_WhenRequestIsAuthorized()
    {
        ActorId actorId =
            ActorId.New();

        Guid activityId =
            Guid.NewGuid();

        DateTimeOffset joinedAt =
            new(
                2026,
                9,
                11,
                10,
                30,
                0,
                TimeSpan.Zero);

        IReadOnlyList<ParticipationListItem> items =
            new[]
            {
                new ParticipationListItem(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    ParticipationStatus.Present,
                    "LATE",
                    joinedAt,
                    null),

                new ParticipationListItem(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    ParticipationStatus.NotRecorded,
                    null,
                    null,
                    null)
            };

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                items);

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

        string requestUri =
            $"/api/participations" +
            $"?activityTypeCode=" +
            $"{ActivityTypeCode.TrainingCode}" +
            $"&activityId={activityId:D}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        List<ParticipationListItemResponse>? responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    List<ParticipationListItemResponse>>(
                    TestContext.Current.CancellationToken);

        responseBody
            .Should()
            .NotBeNull();

        IReadOnlyList<ParticipationListItemResponse>
            expectedResponse =
                new[]
                {
                    new ParticipationListItemResponse(
                        items[0].Id,
                        items[0].AtmacaCardId,
                        "PRESENT",
                        "LATE",
                        joinedAt,
                        null),

                    new ParticipationListItemResponse(
                        items[1].Id,
                        items[1].AtmacaCardId,
                        "NOT_RECORDED",
                        null,
                        null,
                        null)
                };

        responseBody!
            .Should()
            .Equal(
                expectedResponse);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.ListByActivity);

        reader.ListByActivityCallCount
            .Should()
            .Be(1);

        reader.ObservedActivityReference
            .Should()
            .NotBeNull();

        reader.ObservedActivityReference!
            .ActivityType
            .Should()
            .Be(
                ActivityTypeCode.Training);

        reader.ObservedActivityReference
            .ActivityId
            .Should()
            .Be(
                activityId);
    }

    [Fact]
    public async Task
        Get_Should_ReturnOkWithEmptyArray_WhenNoParticipationsMatch()
    {
        ActorId actorId =
            ActorId.New();

        Guid activityId =
            Guid.NewGuid();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                Array.Empty<ParticipationListItem>());

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
                $"/api/participations?activityTypeCode=TRAINING&activityId={activityId:D}",
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!
            .MediaType
            .Should()
            .Be("application/json");

        string responseBody =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument document =
            JsonDocument.Parse(
                responseBody);

        document.RootElement.ValueKind
            .Should()
            .Be(JsonValueKind.Array);

        document.RootElement.GetArrayLength()
            .Should()
            .Be(0);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.ListByActivity);

        reader.ListByActivityCallCount
            .Should()
            .Be(1);
    }

    [Theory]
    [MemberData(nameof(InvalidActivityTypeRequests))]
    public async Task
        Get_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenActivityTypeIsInvalid(
            string activityTypeCode,
            string expectedCode,
            string expectedDetail)
    {
        ActorId actorId =
            ActorId.New();

        Guid activityId =
            Guid.NewGuid();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                Array.Empty<ParticipationListItem>());

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

        string escapedActivityTypeCode =
            Uri.EscapeDataString(
                activityTypeCode);

        string requestUri =
            $"/api/participations" +
            $"?activityTypeCode=" +
            $"{escapedActivityTypeCode}" +
            $"&activityId={activityId:D}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
                TestContext.Current.CancellationToken);

        authorizationService.CallCount
            .Should()
            .Be(0);

        reader.ListByActivityCallCount
            .Should()
            .Be(0);

        reader.ObservedActivityReference
            .Should()
            .BeNull();

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
            .Be(
                (int)HttpStatusCode.BadRequest);

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
                expectedDetail);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                expectedCode);
    }

    [Theory]
    [MemberData(nameof(InvalidActivityIdRequests))]
    public async Task
        Get_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenActivityIdIsInvalid(
            string activityId,
            string expectedCode,
            string expectedDetail)
    {
        ActorId actorId =
            ActorId.New();

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                Array.Empty<ParticipationListItem>());

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

        string escapedActivityId =
            Uri.EscapeDataString(
                activityId);

        string requestUri =
            $"/api/participations" +
            $"?activityTypeCode=" +
            $"{ActivityTypeCode.TrainingCode}" +
            $"&activityId={escapedActivityId}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
                TestContext.Current.CancellationToken);

        authorizationService.CallCount
            .Should()
            .Be(0);

        reader.ListByActivityCallCount
            .Should()
            .Be(0);

        reader.ObservedActivityReference
            .Should()
            .BeNull();

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
            .Be(
                (int)HttpStatusCode.BadRequest);

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
                expectedDetail);

        problem.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                expectedCode);
    }

    [Fact]
    public async Task
        Get_Should_ReturnCanonicalProblemDetailsWithoutReaderAccess_WhenPermissionIsDenied()
    {
        ActorId actorId =
            ActorId.New();

        Guid activityId =
            Guid.NewGuid();

        RecordingActorAuthorizationService
            authorizationService =
                new(
                    shouldDeny: true);

        TrackingParticipationReader reader =
            new(
                Array.Empty<ParticipationListItem>());

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

        string requestUri =
            $"/api/participations" +
            $"?activityTypeCode=" +
            $"{ActivityTypeCode.TrainingCode}" +
            $"&activityId={activityId:D}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
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
            .Be(
                (int)HttpStatusCode.Forbidden);

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
                Permissions.Participations.ListByActivity);

        reader.ListByActivityCallCount
            .Should()
            .Be(0);

        reader.ObservedActivityReference
            .Should()
            .BeNull();
    }

    public static IEnumerable<object[]>
        InvalidActivityTypeRequests()
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
                missingActivityType.Error!.Code,
                missingActivityType.Error.Message
            };

        yield return
            new object[]
            {
                "MATCH",
                unsupportedActivityType.Error!.Code,
                unsupportedActivityType.Error.Message
            };
    }

    public static IEnumerable<object[]>
        InvalidActivityIdRequests()
    {
        const string expectedCode =
            "Participation.ActivityId.Invalid";

        const string expectedDetail =
            "Activity id must be a non-empty GUID in D format.";

        yield return
            new object[]
            {
                "not-a-guid",
                expectedCode,
                expectedDetail
            };

        yield return
            new object[]
            {
                Guid.Empty.ToString(
                    "D"),
                expectedCode,
                expectedDetail
            };
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
                                ListParticipationsByActivityTestAuthenticationHandler>(
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
        private readonly IReadOnlyList<ParticipationListItem>
            _items;

        public TrackingParticipationReader(
            IReadOnlyList<ParticipationListItem> items)
        {
            _items =
                items;
        }

        public int ListByActivityCallCount
        {
            get;
            private set;
        }

        public ActivityReference? ObservedActivityReference
        {
            get;
            private set;
        }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ParticipationListItem>>
            ListByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            ListByActivityCallCount++;

            ObservedActivityReference =
                activityReference;

            return Task.FromResult(
                _items);
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
    ListParticipationsByActivityTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public ListParticipationsByActivityTestAuthenticationHandler(
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
                        "list-participations-by-activity-subject")
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
