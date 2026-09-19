using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Api.Tests.TrainingTypes;

public sealed class TrainingTypesEndpointAuthorizationTests
{
    [Theory]
    [InlineData("POST", "/api/training-types")]
    [InlineData("GET", "/api/training-types")]
    [InlineData("PATCH", "/api/training-types/11111111-1111-1111-1111-111111111111/status")]
    public async Task Endpoint_Should_ReturnForbidden_WhenPermissionIsDenied(
        string methodName,
        string path)
    {
        DenyingAuthorization authorization = new();
        using ProjectAtmacaApiFactory root = new();
        using var factory = root.WithWebHostBuilder(
            builder => builder.ConfigureServices(
                services => Configure(services, authorization)));
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = new(
            new HttpMethod(methodName),
            path);
        if (methodName == "POST")
        {
            request.Content = JsonContent.Create(
                new
                {
                    code = "TACTIC",
                    name = "Taktik",
                    description = "Topla çalışma",
                    displayOrder = 1
                });
        }
        else if (methodName == "PATCH")
        {
            request.Content = JsonContent.Create(new { isActive = false });
        }

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            responseBody);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");
        using JsonDocument problem = JsonDocument.Parse(responseBody);
        problem.RootElement.GetProperty("status").GetInt32()
            .Should().Be(403);
        problem.RootElement.GetProperty("title").GetString()
            .Should().Be("Forbidden");
        problem.RootElement.GetProperty("code").GetString()
            .Should().Be(ActorAuthorizationErrors.Forbidden.Code);
        authorization.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Endpoints_Should_ChallengeUnauthenticatedRequest()
    {
        using ProjectAtmacaApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/training-types",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void Configure(
        IServiceCollection services,
        DenyingAuthorization authorization)
    {
        DecisionHistoryTestAuthentication.AddTo(services);
        services.RemoveAll<IActorIdentityResolver>();
        services.AddSingleton<IActorIdentityResolver>(new Resolver());
        services.RemoveAll<IActorAuthorizationService>();
        services.AddSingleton<IActorAuthorizationService>(authorization);
        services.RemoveAll<ITrainingTypeRepository>();
        services.AddSingleton<ITrainingTypeRepository>(
            new UnexpectedTrainingTypeRepository());
        services.RemoveAll<IUnitOfWork>();
        services.AddSingleton<IUnitOfWork>(
            new UnexpectedUnitOfWork());
    }

    private sealed class Resolver : IActorIdentityResolver
    {
        public Task<Result<ActorId>> ResolveAsync(
            ExternalIdentity externalIdentity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<ActorId>.Success(ActorId.New()));
    }

    private sealed class DenyingAuthorization : IActorAuthorizationService
    {
        public int Calls { get; private set; }

        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(
                Result.Failure(
                    ActorAuthorizationErrors.Forbidden));
        }
    }

    private sealed class UnexpectedTrainingTypeRepository
        : ITrainingTypeRepository
    {
        public Task AddAsync(
            TrainingType trainingType,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected training type write.");

        public Task<TrainingType?> GetByIdAsync(
            TrainingTypeId id,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected training type read.");

        public Task<IReadOnlyList<TrainingType>> ListAsync(
            bool activeOnly,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected training type list.");
    }

    private sealed class UnexpectedUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected persistence.");
    }
}
