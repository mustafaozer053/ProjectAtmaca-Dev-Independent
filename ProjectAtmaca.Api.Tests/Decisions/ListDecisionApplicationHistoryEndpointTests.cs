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
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Decisions;
using ProjectAtmaca.Application.Decisions.ListApplicationHistory;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Api.Tests.Decisions;

public sealed class ListDecisionApplicationHistoryEndpointTests
{
    [Fact]
    public async Task Get_Should_ReturnTransportHistoryThroughProductionHandler_WhenAuthorized()
    {
        DecisionId decisionId = DecisionId.New();
        var item = new DecisionApplicationHistoryItem(
            DecisionApplicationId.New(), decisionId,
            DecisionTargetReference.ForParticipation(ParticipationId.New()),
            DecisionRevision.From(2), new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.Zero));
        var reader = new TrackingReader([item]);
        var authorization = new TrackingAuthorization();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = CreateFactory(root, reader, authorization);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            $"/api/decisions/{decisionId.Value:D}/applications", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        json.RootElement.GetArrayLength().Should().Be(1);
        var row = json.RootElement[0];
        row.GetProperty("decisionApplicationId").GetGuid().Should().Be(item.DecisionApplicationId.Value);
        row.GetProperty("decisionId").GetGuid().Should().Be(decisionId.Value);
        row.GetProperty("targetTypeCode").GetString().Should().Be("PARTICIPATION");
        row.GetProperty("targetId").GetGuid().Should().Be(item.Target.TargetId);
        row.GetProperty("appliedDecisionRevision").GetInt32().Should().Be(2);
        row.GetProperty("appliedAtUtc").GetDateTimeOffset().Should().Be(item.AppliedAtUtc);
        authorization.Calls.Should().Be(1);
        authorization.Permission.Should().BeSameAs(Permissions.Decisions.ListApplicationHistory);
        reader.Calls.Should().Be(1);
        reader.DecisionId.Should().Be(decisionId);
    }

    [Fact]
    public async Task Get_Should_ChallengeUnauthenticatedRequest()
    {
        using var factory = new ProjectAtmacaApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            $"/api/decisions/{Guid.NewGuid():D}/applications", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("11111111111111111111111111111111")]
    public async Task Get_Should_RejectInvalidIdWithoutApplicationAccess(string decisionId)
    {
        var reader = new TrackingReader([]);
        var authorization = new TrackingAuthorization();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = CreateFactory(root, reader, authorization);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            $"/api/decisions/{decisionId}/applications", TestContext.Current.CancellationToken);
        await AssertProblem(response, HttpStatusCode.BadRequest, "Decision.Id.Invalid");
        authorization.Calls.Should().Be(0);
        reader.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Get_Should_ReturnForbiddenWithoutReaderAccess_WhenPermissionDenied()
    {
        var reader = new TrackingReader([]);
        var authorization = new TrackingAuthorization { Denied = true };
        using var root = new ProjectAtmacaApiFactory();
        using var factory = CreateFactory(root, reader, authorization);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            $"/api/decisions/{Guid.NewGuid():D}/applications", TestContext.Current.CancellationToken);
        await AssertProblem(response, HttpStatusCode.Forbidden, ActorAuthorizationErrors.Forbidden.Code);
        authorization.Calls.Should().Be(1);
        authorization.Permission.Should().BeSameAs(Permissions.Decisions.ListApplicationHistory);
        reader.Calls.Should().Be(0);
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        json.RootElement.GetProperty("status").GetInt32().Should().Be((int)status);
        json.RootElement.GetProperty("title").GetString().Should().Be(
            status == HttpStatusCode.Forbidden ? "Forbidden" : "Bad Request");
        json.RootElement.GetProperty("code").GetString().Should().Be(code);
    }

    private static WebApplicationFactory<global::Program> CreateFactory(
        ProjectAtmacaApiFactory root, TrackingReader reader, TrackingAuthorization authorization) =>
        root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IActorIdentityResolver>();
            services.AddScoped<IActorIdentityResolver>(_ => new FixedIdentityResolver());
            services.RemoveAll<IActorAuthorizationService>();
            services.AddScoped<IActorAuthorizationService>(_ => authorization);
            services.RemoveAll<IDecisionApplicationReader>();
            services.AddScoped<IDecisionApplicationReader>(_ => reader);
            DecisionHistoryTestAuthentication.AddTo(services);
        }));

    private sealed class FixedIdentityResolver : IActorIdentityResolver
    {
        public Task<Result<ActorId>> ResolveAsync(ExternalIdentity identity, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<ActorId>.Success(ActorId.New()));
    }

    private sealed class TrackingAuthorization : IActorAuthorizationService
    {
        public bool Denied { get; init; }
        public int Calls { get; private set; }
        public Permission? Permission { get; private set; }
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            Calls++;
            Permission = permission;
            return Task.FromResult(Denied ? Result.Failure(ActorAuthorizationErrors.Forbidden) : Result.Success());
        }
    }

    private sealed class TrackingReader(IReadOnlyList<DecisionApplicationHistoryItem> items) : IDecisionApplicationReader
    {
        public int Calls { get; private set; }
        public DecisionId? DecisionId { get; private set; }
        public Task<IReadOnlyList<DecisionApplicationHistoryItem>> ListHistoryByDecisionAsync(
            DecisionId decisionId, CancellationToken cancellationToken = default)
        {
            Calls++;
            DecisionId = decisionId;
            return Task.FromResult(items);
        }
    }
}

public sealed class DecisionHistoryTestAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string Subject = "decision-history-test-subject";
    internal static void AddTo(IServiceCollection services) =>
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "DecisionHistoryTest";
            options.DefaultChallengeScheme = "DecisionHistoryTest";
            options.DefaultForbidScheme = "DecisionHistoryTest";
        }).AddScheme<AuthenticationSchemeOptions, DecisionHistoryTestAuthentication>("DecisionHistoryTest", _ => { });

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("iss", ProjectAtmacaApiFactory.AuthenticationAuthority),
                new Claim("sub", Subject)
            ], Scheme.Name)), Scheme.Name)));
}
