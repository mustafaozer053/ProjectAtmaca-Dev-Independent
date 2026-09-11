using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectAtmaca.Api.Participations.GetSummaryByActivity;
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
    GetParticipationSummaryByActivityEndpointAuthorizationTests
{
    private const string TestAuthenticationScheme =
        "GetParticipationSummaryByActivityEndpoint";

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
            $"/api/participations/summary" +
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

        ParticipationActivitySummary summary =
            new(
                Total: 6,
                NotRecorded: 1,
                Present: 4,
                Absent: 1);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                summary);

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
            $"/api/participations/summary" +
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

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!.MediaType
            .Should()
            .Be("application/json");

        SummaryResponse? responseBody =
            await response.Content
                .ReadFromJsonAsync<SummaryResponse>(
                    TestContext.Current.CancellationToken);

        responseBody
            .Should()
            .NotBeNull();

        responseBody!
            .Should()
            .Be(
                new SummaryResponse(
                    Total: 6,
                    NotRecorded: 1,
                    Present: 4,
                    Absent: 1));

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations
                    .GetSummaryByActivity);

        reader.GetSummaryByActivityCallCount
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
        Get_Should_ReturnCanonicalForbiddenProblemDetailsWithoutReaderAccess_WhenPermissionIsDenied()
    {
        ActorId actorId =
            ActorId.New();

        Guid activityId =
            Guid.NewGuid();

        ParticipationActivitySummary summary =
            new(
                Total: 0,
                NotRecorded: 0,
                Present: 0,
                Absent: 0);

        RecordingActorAuthorizationService
            authorizationService =
                new(
                    shouldDeny: true);

        TrackingParticipationReader reader =
            new(
                summary);

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
            $"/api/participations/summary" +
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
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!.MediaType
            .Should()
            .Be("application/problem+json");

        ProblemDetails? problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    TestContext.Current.CancellationToken);

        problem
            .Should()
            .NotBeNull();

        problem!.Status
            .Should()
            .Be(
                (int)HttpStatusCode.Forbidden);

        problem.Title
            .Should()
            .Be("Forbidden");

        problem.Detail
            .Should()
            .Be(
                ActorAuthorizationErrors
                    .Forbidden
                    .Message);

        problem.Extensions
            .Should()
            .ContainKey("code");

        problem.Extensions["code"]
            .Should()
            .NotBeNull();

        problem.Extensions["code"]!
            .ToString()
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
                Permissions.Participations
                    .GetSummaryByActivity);

        reader.GetSummaryByActivityCallCount
            .Should()
            .Be(0);

        reader.ObservedActivityReference
            .Should()
            .BeNull();
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

        ParticipationActivitySummary summary =
            new(
                Total: 0,
                NotRecorded: 0,
                Present: 0,
                Absent: 0);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                summary);

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
            $"/api/participations/summary" +
            $"?activityTypeCode=" +
            $"{System.Uri.EscapeDataString(activityTypeCode)}" +
            $"&activityId={activityId:D}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!.MediaType
            .Should()
            .Be("application/problem+json");

        ProblemDetails? problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    TestContext.Current.CancellationToken);

        problem
            .Should()
            .NotBeNull();

        problem!.Status
            .Should()
            .Be(
                (int)HttpStatusCode.BadRequest);

        problem.Title
            .Should()
            .Be("Bad Request");

        problem.Detail
            .Should()
            .Be(
                expectedDetail);

        problem.Extensions
            .Should()
            .ContainKey("code");

        problem.Extensions["code"]
            .Should()
            .NotBeNull();

        problem.Extensions["code"]!
            .ToString()
            .Should()
            .Be(
                expectedCode);

        authorizationService.CallCount
            .Should()
            .Be(0);

        authorizationService.ObservedPermission
            .Should()
            .BeNull();

        reader.GetSummaryByActivityCallCount
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

        ParticipationActivitySummary summary =
            new(
                Total: 0,
                NotRecorded: 0,
                Present: 0,
                Absent: 0);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                summary);

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
            $"/api/participations/summary" +
            $"?activityTypeCode=" +
            $"{ActivityTypeCode.TrainingCode}" +
            $"&activityId=" +
            $"{System.Uri.EscapeDataString(activityId)}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!.MediaType
            .Should()
            .Be("application/problem+json");

        ProblemDetails? problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    TestContext.Current.CancellationToken);

        problem
            .Should()
            .NotBeNull();

        problem!.Status
            .Should()
            .Be(
                (int)HttpStatusCode.BadRequest);

        problem.Title
            .Should()
            .Be("Bad Request");

        problem.Detail
            .Should()
            .Be(
                expectedDetail);

        problem.Extensions
            .Should()
            .ContainKey("code");

        problem.Extensions["code"]
            .Should()
            .NotBeNull();

        problem.Extensions["code"]!
            .ToString()
            .Should()
            .Be(
                expectedCode);

        authorizationService.CallCount
            .Should()
            .Be(0);

        authorizationService.ObservedPermission
            .Should()
            .BeNull();

        reader.GetSummaryByActivityCallCount
            .Should()
            .Be(0);

        reader.ObservedActivityReference
            .Should()
            .BeNull();
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
    [Fact]
    public async Task
        Get_Should_ReturnZeroSummary_WhenNoParticipationsMatchActivity()
    {
        ActorId actorId =
            ActorId.New();

        Guid activityId =
            Guid.NewGuid();

        ParticipationActivitySummary summary =
            new(
                Total: 0,
                NotRecorded: 0,
                Present: 0,
                Absent: 0);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                summary);

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
            $"/api/participations/summary" +
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

        response.Content.Headers.ContentType
            .Should()
            .NotBeNull();

        response.Content.Headers.ContentType!.MediaType
            .Should()
            .Be("application/json");

        ParticipationActivitySummaryResponse? responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    ParticipationActivitySummaryResponse>(
                    TestContext.Current.CancellationToken);

        responseBody
            .Should()
            .NotBeNull();

        responseBody!
            .Should()
            .Be(
                new ParticipationActivitySummaryResponse(
                    Total: 0,
                    NotRecorded: 0,
                    Present: 0,
                    Absent: 0));

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations
                    .GetSummaryByActivity);

        reader.GetSummaryByActivityCallCount
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
                                GetParticipationSummaryByActivityTestAuthenticationHandler>(
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

        public int CallCount
        {
            get;
            private set;
        }

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
        private readonly ParticipationActivitySummary
            _summary;

        public TrackingParticipationReader(
            ParticipationActivitySummary summary)
        {
            _summary =
                summary;
        }

        public int GetSummaryByActivityCallCount
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
            throw new NotSupportedException();
        }

        public Task<ParticipationActivitySummary>
            GetSummaryByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            GetSummaryByActivityCallCount++;

            ObservedActivityReference =
                activityReference;

            return Task.FromResult(
                _summary);
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

    private sealed record SummaryResponse(
        int Total,
        int NotRecorded,
        int Present,
        int Absent);
}

public sealed class
    GetParticipationSummaryByActivityTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public GetParticipationSummaryByActivityTestAuthenticationHandler(
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
                        "get-participation-summary-by-activity-subject")
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
