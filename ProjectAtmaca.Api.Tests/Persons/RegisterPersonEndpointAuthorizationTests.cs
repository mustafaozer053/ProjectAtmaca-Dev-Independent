using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Services;

namespace ProjectAtmaca.Api.Tests.Persons;

public sealed class RegisterPersonEndpointAuthorizationTests
{
    internal static JsonObject ValidRequest() => JsonSerializer.SerializeToNode(new
    {
        operationId = Guid.NewGuid().ToString("D"), fullName = "Test Person", birthDate = "2010-01-02",
        birthCountry = new { code = "TR", name = "Türkiye" }, status = 1, nationalIdentityNumber = "12345678901"
    })!.AsObject();

    [Fact]
    public async Task Post_Should_ReturnForbidden_WithoutRegistrationStoreOrNumberAccess()
    {
        var authorization = new Authorization(false);
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.ConfigureServices(s => Configure(s, authorization)));
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/person-registrations", ValidRequest(), TestContext.Current.CancellationToken);
        await AssertProblem(response, 403, ActorAuthorizationErrors.Forbidden.Code);
        authorization.Calls.Should().Be(1);
        authorization.Permission.Should().BeSameAs(Permissions.Persons.RegisterWithAtmacaCard);
    }

    [Theory]
    [InlineData("birthDate", null)]
    [InlineData("birthDate", "0001-01-01")]
    [InlineData("birthDate", "9999-01-01")]
    [InlineData("birthDate", "2010-01-02T00:00:00Z")]
    [InlineData("birthDate", "2010-02-30")]
    [InlineData("fullName", "")]
    [InlineData("motherName", "x")]
    [InlineData("email", "invalid-private-value")]
    [InlineData("turkishCitizenshipAcquiredOn", "0001-01-01")]
    public async Task Post_Should_RejectInvalidFieldsBeforeApplicationAccess(string field, string? value)
    {
        var request = ValidRequest();
        request[field] = value;
        var authorization = new Authorization(false);
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.ConfigureServices(s => Configure(s, authorization)));
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/person-registrations", request, TestContext.Current.CancellationToken);
        await AssertProblem(response, 400, "PersonRegistration.Request.Invalid");
        authorization.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("{", "Api.Request.Invalid")]
    [InlineData("null", "Api.Request.Invalid")]
    [InlineData("[]", "Api.Request.Invalid")]
    public async Task Post_Should_RejectMalformedBody(string body, string code)
    {
        var authorization = new Authorization(false);
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.ConfigureServices(s => Configure(s, authorization)));
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/api/person-registrations", new StringContent(body, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        await AssertProblem(response, 400, code);
        authorization.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("status", "99")]
    [InlineData("bloodType", "99")]
    [InlineData("birthCountry", "null")]
    [InlineData("birthPlace", "{}")]
    [InlineData("address", "{}")]
    [InlineData("primaryPhoneNumber", "{}")]
    [InlineData("citizenships", "[null]")]
    [InlineData("citizenships", "[{}]")]
    public async Task Post_Should_RejectInvalidNestedValues(string field, string json)
    {
        var request = ValidRequest();
        request[field] = JsonNode.Parse(json);
        var authorization = new Authorization(false);
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.ConfigureServices(s => Configure(s, authorization)));
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/person-registrations", request, TestContext.Current.CancellationToken);
        await AssertProblem(response, 400, "PersonRegistration.Request.Invalid");
        authorization.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("11111111111111111111111111111111")]
    public async Task Post_Should_RejectInvalidOperation(string? operation)
    {
        var request = ValidRequest(); request["operationId"] = operation;
        var authorization = new Authorization(false);
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.ConfigureServices(s => Configure(s, authorization)));
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/person-registrations", request, TestContext.Current.CancellationToken);
        await AssertProblem(response, 400, PersonRegistrationOperationErrors.OperationRequired.Code);
        authorization.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Post_Should_ChallengeUnauthenticatedRequest()
    {
        using var factory = new ProjectAtmacaApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/person-registrations", ValidRequest(), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    internal static async Task AssertProblem(HttpResponseMessage response, int status, string code)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        ((int)response.StatusCode).Should().Be(status, body);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("code").GetString().Should().Be(code);
        body.Should().NotContain("12345678901").And.NotContain("invalid-private-value");
    }

    private static void Configure(IServiceCollection services, Authorization authorization)
    {
        DecisionHistoryTestAuthentication.AddTo(services);
        services.RemoveAll<IActorIdentityResolver>();
        services.AddSingleton<IActorIdentityResolver>(new Resolver());
        services.RemoveAll<IActorAuthorizationService>();
        services.AddSingleton<IActorAuthorizationService>(authorization);
        services.RemoveAll<IPersonRegistrationStore>();
        services.AddSingleton<IPersonRegistrationStore>(new UnexpectedStore());
        services.RemoveAll<IAtmacaCardNumberGenerator>();
        services.AddSingleton<IAtmacaCardNumberGenerator>(new UnexpectedNumber());
    }
    private sealed class Resolver : IActorIdentityResolver
    {
        public Task<Result<ActorId>> ResolveAsync(ExternalIdentity identity, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<ActorId>.Success(ActorId.New()));
    }
    private sealed class Authorization(bool allowed) : IActorAuthorizationService
    {
        public int Calls { get; private set; }
        public Permission? Permission { get; private set; }
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            Calls++; Permission = permission;
            return Task.FromResult(allowed ? Result.Success() : Result.Failure(ActorAuthorizationErrors.Forbidden));
        }
    }
    private sealed class UnexpectedNumber : IAtmacaCardNumberGenerator
    {
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unexpected number allocation.");
    }
    private sealed class UnexpectedStore : IPersonRegistrationStore
    {
        public Task<CompletedPersonRegistration?> FindAsync(ActorId actorId, Guid operationId, CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected store read.");
        public Task<Result<RegistrationReceipt>> CommitAsync(ActorId actorId, Guid operationId, PersonRegistrationInput input,
            PreparedPersonRegistration draft, CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected store write.");
    }
}
