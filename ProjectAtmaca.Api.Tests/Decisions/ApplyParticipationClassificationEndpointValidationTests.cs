using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Decisions.ApplyParticipationClassification;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Tests.Decisions;

public sealed class ApplyParticipationClassificationEndpointValidationTests
{
    [Theory]
    [InlineData("not-an-integer")]
    [InlineData("2147483648")]
    public async Task Get_Should_ReturnCanonicalBindingProblem_WhenPageSizeCannotBind(string pageSize)
    {
        var authorization = new DenyingAuthorization();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services => Configure(services, authorization)));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            $"/api/participations/history?atmacaCardId={Guid.NewGuid():D}&pageSize={pageSize}",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        json.RootElement.GetProperty("code").GetString().Should().Be("Api.Request.Invalid");
        json.RootElement.GetProperty("title").GetString().Should().Be("Bad Request");
        json.RootElement.GetProperty("errors").GetProperty("pageSize").GetArrayLength().Should().Be(1);
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        authorization.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("decision", "{")]
    [InlineData("decision", "")]
    [InlineData("decision", "null")]
    [InlineData("decision", "{\"decisionRevision\":{}}")]
    [InlineData("participation", "{")]
    [InlineData("participation", "")]
    [InlineData("participation", "null")]
    [InlineData("participation", "{\"activityTypeCode\":\"TRAINING\",\"activityId\":{},\"atmacaCardId\":\"11111111-1111-1111-1111-111111111111\"}")]
    public async Task Post_Should_RejectUnbindableJsonBeforeApplicationAuthorization(string endpoint, string payload)
    {
        var authorization = new DenyingAuthorization();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services => Configure(services, authorization)));
        using var client = factory.CreateClient();
        string uri = endpoint == "decision"
            ? $"/api/decisions/{Guid.NewGuid():D}/apply-participation-classification"
            : "/api/participations";
        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(uri, content, TestContext.Current.CancellationToken);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        json.RootElement.GetProperty("errors").EnumerateObject().Should().NotBeEmpty();
        json.RootElement.GetProperty("code").GetString().Should().Be("Api.Request.Invalid");
        json.RootElement.GetProperty("title").GetString().Should().Be("Bad Request");
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        body.Should().NotContain("ProjectAtmaca.Api");
        body.Should().NotContain("BytePositionInLine");
        authorization.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("decision", "bad", "Decision.Id.Invalid")]
    [InlineData("decision", "00000000-0000-0000-0000-000000000000", "Decision.Id.Invalid")]
    [InlineData("operation", null, "DecisionApplication.OperationId.Invalid")]
    [InlineData("operation", "00000000-0000-0000-0000-000000000000", "DecisionApplication.OperationId.Invalid")]
    [InlineData("operation", "11111111111111111111111111111111", "DecisionApplication.OperationId.Invalid")]
    [InlineData("revision", "0", "Decision.Revision.Invalid")]
    [InlineData("revision", "-1", "Decision.Revision.Invalid")]
    [InlineData("time", null, "DecisionApplication.AppliedAtUtc.Invalid")]
    [InlineData("time", "not-a-date", "DecisionApplication.AppliedAtUtc.Invalid")]
    [InlineData("time", "2026-09-13T10:00:00", "DecisionApplication.AppliedAtUtc.Invalid")]
    [InlineData("time", "2026-09-13T10:00:00+03:00", "DecisionApplication.AppliedAtUtc.Invalid")]
    [InlineData("time", "0001-01-01T00:00:00Z", "DecisionApplication.AppliedAtUtc.Invalid")]
    public async Task Post_Should_RejectInvalidTransportWithoutApplicationAccess(string field, string? value, string code)
    {
        var authorization = new DenyingAuthorization();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services => Configure(services, authorization)));
        using var client = factory.CreateClient();
        string decisionId = field == "decision" ? value! : Guid.NewGuid().ToString("D");
        using var response = await client.PostAsJsonAsync(
            $"/api/decisions/{decisionId}/apply-participation-classification", new
            {
                operationId = field == "operation" ? value : Guid.NewGuid().ToString("D"),
                decisionRevision = field == "revision" ? int.Parse(value!) : 1,
                appliedAtUtc = field == "time" ? value : "2026-09-13T10:00:00Z"
            }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        json.RootElement.GetProperty("code").GetString().Should().Be(code);
        json.RootElement.GetProperty("title").GetString().Should().Be("Bad Request");
        authorization.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("2026-09-13T10:00:00Z")]
    [InlineData("2026-09-13T10:00:00+00:00")]
    [InlineData("2026-09-13T10:00:00.1234567Z")]
    public async Task Post_Should_AuthorizeValidUtcRequestWithoutStoreAccess_WhenDenied(string time)
    {
        var authorization = new DenyingAuthorization();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services => Configure(services, authorization)));
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            $"/api/decisions/{Guid.NewGuid():D}/apply-participation-classification",
            new { operationId = Guid.NewGuid(), decisionRevision = 1, appliedAtUtc = time },
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        json.RootElement.GetProperty("code").GetString().Should().Be(ActorAuthorizationErrors.Forbidden.Code);
        authorization.Calls.Should().Be(1);
        authorization.Permission.Should().BeSameAs(Permissions.Decisions.ApplyParticipationClassification);
    }

    [Fact]
    public async Task Post_Should_ChallengeUnauthenticatedRequest()
    {
        using var factory = new ProjectAtmacaApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            $"/api/decisions/{Guid.NewGuid():D}/apply-participation-classification",
            new { operationId = Guid.NewGuid(), decisionRevision = 1, appliedAtUtc = "2026-09-13T10:00:00Z" },
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void Configure(IServiceCollection services, DenyingAuthorization authorization)
    {
        DecisionHistoryTestAuthentication.AddTo(services);
        services.RemoveAll<IActorIdentityResolver>();
        services.AddScoped<IActorIdentityResolver>(_ => new FixedResolver());
        services.RemoveAll<IActorAuthorizationService>();
        services.AddScoped<IActorAuthorizationService>(_ => authorization);
        services.RemoveAll<IDecisionApplicationOperationStore>();
        services.AddScoped<IDecisionApplicationOperationStore, UnexpectedStore>();
    }

    private sealed class FixedResolver : IActorIdentityResolver
    {
        public Task<Result<ActorId>> ResolveAsync(ExternalIdentity identity, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<ActorId>.Success(ActorId.New()));
    }
    private sealed class DenyingAuthorization : IActorAuthorizationService
    {
        public int Calls { get; private set; }
        public Permission? Permission { get; private set; }
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            Calls++;
            Permission = permission;
            return Task.FromResult(Result.Failure(ActorAuthorizationErrors.Forbidden));
        }
    }
    private sealed class UnexpectedStore : IDecisionApplicationOperationStore
    {
        public Task<DecisionApplicationOperation?> GetByIdAsync(DecisionApplicationOperationId id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Operation store must not be accessed.");
        public Task AddAsync(DecisionApplicationOperation operation, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Operation store must not be accessed.");
    }
}
