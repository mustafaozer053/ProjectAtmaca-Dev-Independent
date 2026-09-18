using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Tests.Persons;

public sealed class RegisterPersonEndpointSafetyTests
{
    [Theory]
    [InlineData("Development", "application/json")]
    [InlineData("Production", "text/html")]
    public async Task Post_Should_HideUnexpectedExceptionDetails(string environment, string accept)
    {
        var authorization = new Authorization(true);
        var store = new ThrowingStore();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.UseEnvironment(environment)
            .UseSetting("ConnectionStrings:ProjectAtmacaDatabase",
                "Server=(localdb)\\mssqllocaldb;Database=UnusedSafetyTest;Trusted_Connection=True")
            .ConfigureServices(s => Configure(s, authorization, store)));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        using var response = await client.PostAsync("/api/person-registrations",
            new StringContent(RegisterPersonEndpointAuthorizationTests.ValidRequest().ToJsonString(), Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        await RegisterPersonEndpointAuthorizationTests.AssertProblem(response, 500, "Api.UnexpectedError");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContain("secret-sql").And.NotContain("ThrowingStore").And.NotContain("InvalidOperationException")
            .And.NotContain("private-inner-detail");
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        authorization.Calls.Should().Be(1); store.Calls.Should().Be(1);
    }

    [Theory]
    [InlineData(65535, true, 403)]
    [InlineData(65536, true, 403)]
    [InlineData(65537, true, 413)]
    [InlineData(65535, false, 403)]
    [InlineData(65536, false, 403)]
    [InlineData(65537, false, 413)]
    public async Task Post_Should_EnforceByteLimitBeforeApplicationAccess(int length, bool knownLength, int expectedStatus)
    {
        var authorization = new Authorization(false);
        var store = new ThrowingStore();
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(b => b.ConfigureServices(s => Configure(s, authorization, store)));
        using var client = factory.CreateClient();
        var request = RegisterPersonEndpointAuthorizationTests.ValidRequest();
        request["ignoredNote"] = "ğ"; // Count UTF-8 bytes, not characters.
        byte[] json = Encoding.UTF8.GetBytes(request.ToJsonString());
        var body = new byte[length];
        Array.Fill(body, (byte)' ');
        json.CopyTo(body, 0);
        using HttpContent content = knownLength ? new ByteArrayContent(body) : new UnknownLengthContent(body);
        content.Headers.ContentType = new("application/json");
        using var response = await client.PostAsync("/api/person-registrations", content, TestContext.Current.CancellationToken);
        await RegisterPersonEndpointAuthorizationTests.AssertProblem(response, expectedStatus,
            expectedStatus == 413 ? "Api.Request.TooLarge" : ActorAuthorizationErrors.Forbidden.Code);
        authorization.Calls.Should().Be(expectedStatus == 413 ? 0 : 1);
        store.Calls.Should().Be(0);
    }

    private static void Configure(IServiceCollection services, Authorization authorization, ThrowingStore store)
    {
        DecisionHistoryTestAuthentication.AddTo(services);
        services.RemoveAll<IActorIdentityResolver>();
        services.AddSingleton<IActorIdentityResolver>(new Resolver());
        services.RemoveAll<IActorAuthorizationService>();
        services.AddSingleton<IActorAuthorizationService>(authorization);
        services.RemoveAll<IPersonRegistrationStore>();
        services.AddSingleton<IPersonRegistrationStore>(store);
    }
    private sealed class Resolver : IActorIdentityResolver
    {
        public Task<Result<ActorId>> ResolveAsync(ExternalIdentity identity, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<ActorId>.Success(ActorId.New()));
    }
    private sealed class Authorization(bool allowed) : IActorAuthorizationService
    {
        public int Calls { get; private set; }
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(allowed ? Result.Success() : Result.Failure(ActorAuthorizationErrors.Forbidden));
        }
    }
    private sealed class ThrowingStore : IPersonRegistrationStore
    {
        public int Calls { get; private set; }
        public Task<CompletedPersonRegistration?> FindAsync(ActorId actorId, Guid operationId, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("secret-sql 12345678901", new Exception("private-inner-detail"));
        }
        public Task<Result<RegistrationReceipt>> CommitAsync(ActorId actorId, Guid operationId, PersonRegistrationInput input,
            PreparedPersonRegistration draft, CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected commit.");
    }
    private sealed class UnknownLengthContent(byte[] body) : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(body).AsTask();
    }
}
