using System.Net;
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
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class
    ListParticipationHistoryByAtmacaCardEndpointAuthorizationTests
{
    private const string TestAuthenticationScheme =
        "ListParticipationHistoryByAtmacaCardEndpoint";

    [Fact]
    public async Task
        Get_Should_ChallengeUnauthenticatedRequest()
    {
        using ProjectAtmacaApiFactory factory =
            new();

        using HttpClient client =
            factory.CreateClient();

        Guid atmacaCardId =
            Guid.NewGuid();

        string requestUri =
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardId:D}" +
            $"&pageSize=25";

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
        Get_Should_ReturnTransportPageThroughProductionHandler_WhenRequestIsAuthorized()
    {
        ActorId actorId =
            ActorId.New();

        Guid atmacaCardIdValue =
            Guid.NewGuid();

        AtmacaCardId atmacaCardId =
            AtmacaCardId.From(
                atmacaCardIdValue);

        Guid participationId =
            Guid.NewGuid();

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

        DateTimeOffset leftAt =
            new(
                2026,
                9,
                11,
                12,
                15,
                0,
                TimeSpan.Zero);

        DateTime createdAtUtc =
            new(
                2026,
                9,
                11,
                9,
                45,
                0,
                DateTimeKind.Utc);

        Guid nextCursorParticipationId =
            Guid.NewGuid();

        DateTime nextCursorCreatedAtUtc =
            new(
                2026,
                9,
                10,
                14,
                0,
                0,
                DateTimeKind.Utc);

        ParticipationHistoryItem item =
            new(
                participationId,
                ActivityReference.ForTraining(
                    TrainingId.From(
                        activityId)),
                ParticipationStatus.Present,
                ParticipationCondition.Late.Code,
                joinedAt,
                leftAt,
                createdAtUtc);

        ParticipationHistoryCursor nextCursor =
            new(
                atmacaCardId,
                nextCursorCreatedAtUtc,
                nextCursorParticipationId);

        ParticipationHistoryPage page =
            new(
                new[]
                {
                    item
                },
                nextCursor);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                page);

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

        const int pageSize =
            25;

        string requestUri =
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardIdValue:D}" +
            $"&pageSize={pageSize}";

        using HttpResponseMessage response =
            await client.GetAsync(
                requestUri,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType?
            .MediaType
            .Should()
            .Be("application/json");

        string responseJson =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument responseDocument =
            JsonDocument.Parse(
                responseJson);

        JsonElement root =
            responseDocument.RootElement;

        JsonElement items =
            root.GetProperty(
                "items");

        items.ValueKind
            .Should()
            .Be(JsonValueKind.Array);

        items.GetArrayLength()
            .Should()
            .Be(1);

        JsonElement responseItem =
            items[0];

        responseItem
            .GetProperty(
                "participationId")
            .GetGuid()
            .Should()
            .Be(participationId);

        responseItem
            .GetProperty(
                "activityTypeCode")
            .GetString()
            .Should()
            .Be(
                ActivityTypeCode.TrainingCode);

        responseItem
            .GetProperty(
                "activityId")
            .GetGuid()
            .Should()
            .Be(activityId);

        responseItem
            .GetProperty(
                "statusCode")
            .GetString()
            .Should()
            .Be("PRESENT");

        responseItem
            .GetProperty(
                "conditionCode")
            .GetString()
            .Should()
            .Be(
                ParticipationCondition.Late.Code);

        responseItem
            .GetProperty(
                "joinedAt")
            .GetDateTimeOffset()
            .Should()
            .Be(joinedAt);

        responseItem
            .GetProperty(
                "leftAt")
            .GetDateTimeOffset()
            .Should()
            .Be(leftAt);

        responseItem
            .GetProperty(
                "createdAtUtc")
            .GetDateTime()
            .Should()
            .Be(createdAtUtc);

        JsonElement responseNextCursor =
            root.GetProperty(
                "nextCursor");

        responseNextCursor.ValueKind
            .Should()
            .Be(JsonValueKind.Object);

        responseNextCursor
            .GetProperty(
                "atmacaCardId")
            .GetGuid()
            .Should()
            .Be(atmacaCardIdValue);

        responseNextCursor
            .GetProperty(
                "createdAtUtc")
            .GetDateTime()
            .Should()
            .Be(nextCursorCreatedAtUtc);

        responseNextCursor
            .GetProperty(
                "participationId")
            .GetGuid()
            .Should()
            .Be(nextCursorParticipationId);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations
                    .ListHistoryByAtmacaCard);

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(1);

        reader.ObservedAtmacaCardId
            .Should()
            .Be(atmacaCardId);

        reader.ObservedPageSize
            .Should()
            .Be(pageSize);

        reader.ObservedCursor
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task
        Get_Should_ReturnCanonicalForbiddenProblemDetailsWithoutReaderAccess_WhenPermissionIsDenied()
    {
        ActorId actorId =
            ActorId.New();

        Guid atmacaCardIdValue =
            Guid.NewGuid();

        ParticipationHistoryPage page =
            new(
                Array.Empty<ParticipationHistoryItem>(),
                null);

        RecordingActorAuthorizationService
            authorizationService =
                new(
                    shouldDeny: true);

        TrackingParticipationReader reader =
            new(
                page);

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
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardIdValue:D}" +
            $"&pageSize=25";

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

        string responseJson =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument responseDocument =
            JsonDocument.Parse(
                responseJson);

        JsonElement problem =
            responseDocument.RootElement;

        problem
            .GetProperty(
                "status")
            .GetInt32()
            .Should()
            .Be(
                (int)HttpStatusCode.Forbidden);

        problem
            .GetProperty(
                "title")
            .GetString()
            .Should()
            .Be("Forbidden");

        problem
            .GetProperty(
                "detail")
            .GetString()
            .Should()
            .Be(
                ActorAuthorizationErrors
                    .Forbidden
                    .Message);

        problem
            .GetProperty(
                "code")
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
                Permissions.Participations
                    .ListHistoryByAtmacaCard);

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(0);

        reader.ObservedAtmacaCardId
            .Should()
            .BeNull();

        reader.ObservedCursor
            .Should()
            .BeNull();
    }
    [Theory]
    [InlineData(
        "not-a-guid",
        "Participation.AtmacaCardId.Invalid",
        "Atmaca card id must be a non-empty GUID in D format.")]
    [InlineData(
        "00000000-0000-0000-0000-000000000000",
        "Participation.AtmacaCardId.Invalid",
        "Atmaca card id must be a non-empty GUID in D format.")]
    public async Task
        Get_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenAtmacaCardIdIsInvalid(
            string atmacaCardId,
            string expectedCode,
            string expectedDetail)
    {
        ActorId actorId =
            ActorId.New();

        ParticipationHistoryPage page =
            new(
                Array.Empty<ParticipationHistoryItem>(),
                null);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                page);

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
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardId}" +
            $"&pageSize=25";

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

        string responseJson =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument responseDocument =
            JsonDocument.Parse(
                responseJson);

        JsonElement problem =
            responseDocument.RootElement;

        problem
            .GetProperty(
                "status")
            .GetInt32()
            .Should()
            .Be(
                (int)HttpStatusCode.BadRequest);

        problem
            .GetProperty(
                "title")
            .GetString()
            .Should()
            .Be("Bad Request");

        problem
            .GetProperty(
                "detail")
            .GetString()
            .Should()
            .Be(expectedDetail);

        problem
            .GetProperty(
                "code")
            .GetString()
            .Should()
            .Be(expectedCode);

        authorizationService.CallCount
            .Should()
            .Be(0);

        authorizationService.ObservedPermission
            .Should()
            .BeNull();

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(0);

        reader.ObservedAtmacaCardId
            .Should()
            .BeNull();

        reader.ObservedCursor
            .Should()
            .BeNull();
    }
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task
        Get_Should_ReturnCanonicalBadRequestWithoutReaderAccess_WhenPageSizeIsOutsideSupportedRange(
            int pageSize)
    {
        ActorId actorId =
            ActorId.New();

        Guid atmacaCardIdValue =
            Guid.NewGuid();

        ParticipationHistoryPage page =
            new(
                Array.Empty<ParticipationHistoryItem>(),
                null);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                page);

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
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardIdValue:D}" +
            $"&pageSize={pageSize}";

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

        string responseJson =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument responseDocument =
            JsonDocument.Parse(
                responseJson);

        JsonElement problem =
            responseDocument.RootElement;

        problem
            .GetProperty(
                "status")
            .GetInt32()
            .Should()
            .Be(
                (int)HttpStatusCode.BadRequest);

        problem
            .GetProperty(
                "title")
            .GetString()
            .Should()
            .Be("Bad Request");

        problem
            .GetProperty(
                "detail")
            .GetString()
            .Should()
            .Be(
                ParticipationHistoryErrors
                    .InvalidPageSize
                    .Message);

        problem
            .GetProperty(
                "code")
            .GetString()
            .Should()
            .Be(
                ParticipationHistoryErrors
                    .InvalidPageSize
                    .Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations
                    .ListHistoryByAtmacaCard);

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(0);

        reader.ObservedAtmacaCardId
            .Should()
            .BeNull();

        reader.ObservedCursor
            .Should()
            .BeNull();
    }
    [Fact]
    public async Task
        Get_Should_ReturnEmptyPage_WhenNoHistoryMatchesAtmacaCard()
    {
        ActorId actorId =
            ActorId.New();

        Guid atmacaCardIdValue =
            Guid.NewGuid();

        AtmacaCardId atmacaCardId =
            AtmacaCardId.From(
                atmacaCardIdValue);

        ParticipationHistoryPage page =
            new(
                Array.Empty<ParticipationHistoryItem>(),
                null);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                page);

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

        const int pageSize =
            25;

        string requestUri =
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardIdValue:D}" +
            $"&pageSize={pageSize}";

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

        string responseJson =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument responseDocument =
            JsonDocument.Parse(
                responseJson);

        JsonElement root =
            responseDocument.RootElement;

        root.ValueKind
            .Should()
            .Be(JsonValueKind.Object);

        JsonElement items =
            root.GetProperty(
                "items");

        items.ValueKind
            .Should()
            .Be(JsonValueKind.Array);

        items.GetArrayLength()
            .Should()
            .Be(0);

        root
            .GetProperty(
                "nextCursor")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Null);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations
                    .ListHistoryByAtmacaCard);

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(1);

        reader.ObservedAtmacaCardId
            .Should()
            .Be(atmacaCardId);

        reader.ObservedPageSize
            .Should()
            .Be(pageSize);

        reader.ObservedCursor
            .Should()
            .BeNull();
    }
    [Fact]
    public async Task
        Get_Should_ForwardCompleteCursorWithoutLoss_WhenCursorIsValid()
    {
        ActorId actorId =
            ActorId.New();

        Guid atmacaCardIdValue =
            Guid.NewGuid();

        AtmacaCardId atmacaCardId =
            AtmacaCardId.From(
                atmacaCardIdValue);

        DateTime cursorCreatedAtUtc =
            new(
                2026,
                9,
                10,
                14,
                30,
                45,
                DateTimeKind.Utc);

        Guid cursorParticipationId =
            Guid.NewGuid();

        ParticipationHistoryPage page =
            new(
                Array.Empty<ParticipationHistoryItem>(),
                null);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                page);

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

        const int pageSize =
            25;

        string encodedCursorCreatedAtUtc =
            Uri.EscapeDataString(
                cursorCreatedAtUtc.ToString(
                    "O"));

        string requestUri =
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardIdValue:D}" +
            $"&pageSize={pageSize}" +
            $"&cursorAtmacaCardId={atmacaCardIdValue:D}" +
            $"&cursorCreatedAtUtc={encodedCursorCreatedAtUtc}" +
            $"&cursorParticipationId={cursorParticipationId:D}";

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

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations
                    .ListHistoryByAtmacaCard);

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(1);

        reader.ObservedAtmacaCardId
            .Should()
            .Be(atmacaCardId);

        reader.ObservedPageSize
            .Should()
            .Be(pageSize);

        reader.ObservedCursor
            .Should()
            .NotBeNull();

        reader.ObservedCursor!.AtmacaCardId
            .Should()
            .Be(atmacaCardId);

        reader.ObservedCursor.CreatedAtUtc
            .Should()
            .Be(cursorCreatedAtUtc);

        reader.ObservedCursor.ParticipationId
            .Should()
            .Be(cursorParticipationId);
    }
    [Theory]
    [MemberData(nameof(InvalidCursorRequests))]
    public async Task
        Get_Should_ReturnCanonicalBadRequestWithoutApplicationAccess_WhenCursorIsInvalid(
            string cursorQuery)
    {
        ActorId actorId =
            ActorId.New();

        Guid atmacaCardIdValue =
            Guid.NewGuid();

        ParticipationHistoryPage page =
            new(
                Array.Empty<ParticipationHistoryItem>(),
                null);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                page);

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
            $"/api/participations/history" +
            $"?atmacaCardId={atmacaCardIdValue:D}" +
            $"&pageSize=25" +
            cursorQuery;

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

        string responseJson =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument responseDocument =
            JsonDocument.Parse(
                responseJson);

        JsonElement problem =
            responseDocument.RootElement;

        problem
            .GetProperty(
                "status")
            .GetInt32()
            .Should()
            .Be(
                (int)HttpStatusCode.BadRequest);

        problem
            .GetProperty(
                "title")
            .GetString()
            .Should()
            .Be("Bad Request");

        problem
            .GetProperty(
                "detail")
            .GetString()
            .Should()
            .Be(
                "Cursor must contain a valid Atmaca Card id, " +
                "UTC creation timestamp, and non-empty " +
                "participation id.");

        problem
            .GetProperty(
                "code")
            .GetString()
            .Should()
            .Be(
                "ParticipationHistory.Cursor.Invalid");

        authorizationService.CallCount
            .Should()
            .Be(0);

        authorizationService.ObservedPermission
            .Should()
            .BeNull();

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(0);

        reader.ObservedAtmacaCardId
            .Should()
            .BeNull();

        reader.ObservedCursor
            .Should()
            .BeNull();
    }

    public static IEnumerable<object[]>
        InvalidCursorRequests()
    {
        string validCursorAtmacaCardId =
            Guid.NewGuid().ToString(
                "D");

        string validCursorCreatedAtUtc =
            Uri.EscapeDataString(
                new DateTime(
                    2026,
                    9,
                    10,
                    14,
                    30,
                    45,
                    DateTimeKind.Utc)
                    .ToString(
                        "O"));

        string validCursorParticipationId =
            Guid.NewGuid().ToString(
                "D");

        string nonUtcCursorCreatedAt =
            Uri.EscapeDataString(
                new DateTime(
                    2026,
                    9,
                    10,
                    14,
                    30,
                    45,
                    DateTimeKind.Unspecified)
                    .ToString(
                        "O"));

        yield return
            new object[]
            {
                $"&cursorCreatedAtUtc={validCursorCreatedAtUtc}" +
                $"&cursorParticipationId=" +
                $"{validCursorParticipationId}"
            };

        yield return
            new object[]
            {
                $"&cursorAtmacaCardId={validCursorAtmacaCardId}" +
                $"&cursorParticipationId=" +
                $"{validCursorParticipationId}"
            };

        yield return
            new object[]
            {
                $"&cursorAtmacaCardId={validCursorAtmacaCardId}" +
                $"&cursorCreatedAtUtc={validCursorCreatedAtUtc}"
            };

        yield return
            new object[]
            {
                "&cursorAtmacaCardId=not-a-guid" +
                $"&cursorCreatedAtUtc={validCursorCreatedAtUtc}" +
                $"&cursorParticipationId=" +
                $"{validCursorParticipationId}"
            };

        yield return
            new object[]
            {
                "&cursorAtmacaCardId=" +
                "00000000-0000-0000-0000-000000000000" +
                $"&cursorCreatedAtUtc={validCursorCreatedAtUtc}" +
                $"&cursorParticipationId=" +
                $"{validCursorParticipationId}"
            };

        yield return
            new object[]
            {
                $"&cursorAtmacaCardId={validCursorAtmacaCardId}" +
                "&cursorCreatedAtUtc=not-a-date" +
                $"&cursorParticipationId=" +
                $"{validCursorParticipationId}"
            };

        yield return
            new object[]
            {
                $"&cursorAtmacaCardId={validCursorAtmacaCardId}" +
                $"&cursorCreatedAtUtc={nonUtcCursorCreatedAt}" +
                $"&cursorParticipationId=" +
                $"{validCursorParticipationId}"
            };

        yield return
            new object[]
            {
                $"&cursorAtmacaCardId={validCursorAtmacaCardId}" +
                $"&cursorCreatedAtUtc={validCursorCreatedAtUtc}" +
                "&cursorParticipationId=not-a-guid"
            };

        yield return
            new object[]
            {
                $"&cursorAtmacaCardId={validCursorAtmacaCardId}" +
                $"&cursorCreatedAtUtc={validCursorCreatedAtUtc}" +
                "&cursorParticipationId=" +
                "00000000-0000-0000-0000-000000000000"
            };
    }
    [Fact]
    public async Task
        Get_Should_ReturnCanonicalBadRequestWithoutReaderAccess_WhenCursorScopeDoesNotMatchAtmacaCard()
    {
        ActorId actorId =
            ActorId.New();

        Guid requestedAtmacaCardIdValue =
            Guid.Parse(
                "11111111-1111-4111-8111-111111111111");

        Guid cursorAtmacaCardIdValue =
            Guid.Parse(
                "22222222-2222-4222-8222-222222222222");

        DateTime cursorCreatedAtUtc =
            new(
                2026,
                9,
                10,
                14,
                30,
                45,
                DateTimeKind.Utc);

        Guid cursorParticipationId =
            Guid.NewGuid();

        ParticipationHistoryPage page =
            new(
                Array.Empty<ParticipationHistoryItem>(),
                null);

        RecordingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new(
                page);

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

        string encodedCursorCreatedAtUtc =
            Uri.EscapeDataString(
                cursorCreatedAtUtc.ToString(
                    "O"));

        string requestUri =
            $"/api/participations/history" +
            $"?atmacaCardId={requestedAtmacaCardIdValue:D}" +
            $"&pageSize=25" +
            $"&cursorAtmacaCardId={cursorAtmacaCardIdValue:D}" +
            $"&cursorCreatedAtUtc={encodedCursorCreatedAtUtc}" +
            $"&cursorParticipationId={cursorParticipationId:D}";

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

        string responseJson =
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        using JsonDocument responseDocument =
            JsonDocument.Parse(
                responseJson);

        JsonElement problem =
            responseDocument.RootElement;

        problem
            .GetProperty(
                "status")
            .GetInt32()
            .Should()
            .Be(
                (int)HttpStatusCode.BadRequest);

        problem
            .GetProperty(
                "title")
            .GetString()
            .Should()
            .Be("Bad Request");

        problem
            .GetProperty(
                "detail")
            .GetString()
            .Should()
            .Be(
                ParticipationHistoryErrors
                    .CursorScopeMismatch
                    .Message);

        problem
            .GetProperty(
                "code")
            .GetString()
            .Should()
            .Be(
                ParticipationHistoryErrors
                    .CursorScopeMismatch
                    .Code);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations
                    .ListHistoryByAtmacaCard);

        reader.ListHistoryByAtmacaCardCallCount
            .Should()
            .Be(0);

        reader.ObservedAtmacaCardId
            .Should()
            .BeNull();

        reader.ObservedCursor
            .Should()
            .BeNull();
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
                                ListParticipationHistoryByAtmacaCardTestAuthenticationHandler>(
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
        private readonly ActorId
            _actorId;

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
        private readonly ParticipationHistoryPage
            _page;

        public TrackingParticipationReader(
            ParticipationHistoryPage page)
        {
            _page =
                page;
        }

        public int ListHistoryByAtmacaCardCallCount
        {
            get;
            private set;
        }

        public AtmacaCardId? ObservedAtmacaCardId
        {
            get;
            private set;
        }

        public int ObservedPageSize
        {
            get;
            private set;
        }

        public ParticipationHistoryCursor? ObservedCursor
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
            throw new NotSupportedException();
        }

        public Task<ParticipationHistoryPage>
            ListHistoryByAtmacaCardAsync(
                AtmacaCardId atmacaCardId,
                int pageSize,
                ParticipationHistoryCursor? cursor,
                CancellationToken cancellationToken = default)
        {
            ListHistoryByAtmacaCardCallCount++;

            ObservedAtmacaCardId =
                atmacaCardId;

            ObservedPageSize =
                pageSize;

            ObservedCursor =
                cursor;

            return Task.FromResult(
                _page);
        }
    }
}

public sealed class
    ListParticipationHistoryByAtmacaCardTestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public ListParticipationHistoryByAtmacaCardTestAuthenticationHandler(
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
                        "list-participation-history-subject")
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
