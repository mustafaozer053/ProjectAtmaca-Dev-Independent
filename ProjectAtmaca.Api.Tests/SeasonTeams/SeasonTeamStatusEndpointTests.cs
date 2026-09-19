using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.SeasonTeams;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Api.Tests.SeasonTeams;

public sealed class SeasonTeamStatusEndpointTests
{
    [Fact]
    public async Task Patch_Should_DeactivateSeasonTeam_AndReturnNoContent()
    {
        SeasonTeam seasonTeam = CreateSeasonTeam();
        InMemorySeasonTeamRepository repository = new(seasonTeam);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/season-teams/{seasonTeam.SeasonTeamId.Value:D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        seasonTeam.Status.Should().Be(SeasonTeamStatus.Inactive);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Patch_Should_ReturnNotFound_WhenSeasonTeamDoesNotExist()
    {
        using var factory = CreateFactory(new InMemorySeasonTeamRepository());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/season-teams/{Guid.NewGuid():D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        await AssertProblem(
            response,
            HttpStatusCode.NotFound,
            SeasonTeamApplicationErrors.NotFound.Code);
    }

    [Fact]
    public async Task Patch_Should_ReturnForbidden_WhenPermissionIsDenied()
    {
        DenyingAuthorization authorization = new();
        InMemorySeasonTeamRepository repository = new(CreateSeasonTeam());
        using var factory = CreateFactory(repository, authorization);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/season-teams/{repository.SeasonTeam!.SeasonTeamId.Value:D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        await AssertProblem(
            response,
            HttpStatusCode.Forbidden,
            ActorAuthorizationErrors.Forbidden.Code);
        authorization.Calls.Should().Be(1);
        repository.GetByIdCalls.Should().Be(0);
    }

    [Fact]
    public async Task Patch_Should_ReturnUnauthorized_WhenRequestIsUnauthenticated()
    {
        using ProjectAtmacaApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/season-teams/{Guid.NewGuid():D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static WebApplicationFactory<global::Program> CreateFactory(
        InMemorySeasonTeamRepository repository,
        IActorAuthorizationService? authorization = null)
    {
        ProjectAtmacaApiFactory root = new();
        return root.WithWebHostBuilder(
            builder => builder.ConfigureServices(
                services =>
                {
                    DecisionHistoryTestAuthentication.AddTo(services);
                    services.RemoveAll<IActorIdentityResolver>();
                    services.AddSingleton<IActorIdentityResolver>(
                        new Resolver());
                    services.RemoveAll<IActorAuthorizationService>();
                    services.AddSingleton(
                        authorization ?? new AllowingAuthorization());
                    services.RemoveAll<ISeasonTeamRepository>();
                    services.AddSingleton<ISeasonTeamRepository>(repository);
                    services.RemoveAll<IUnitOfWork>();
                    services.AddSingleton<IUnitOfWork>(repository);
                }));
    }

    private static SeasonTeam CreateSeasonTeam() =>
        SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "U16").Value!;

    private static async Task AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode status,
        string code)
    {
        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(status, body);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");
        using JsonDocument problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32()
            .Should().Be((int)status);
        problem.RootElement.GetProperty("code").GetString().Should().Be(code);
    }

    private sealed class Resolver : IActorIdentityResolver
    {
        public Task<Result<ActorId>> ResolveAsync(
            ExternalIdentity externalIdentity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<ActorId>.Success(ActorId.New()));
    }

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
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
                Result.Failure(ActorAuthorizationErrors.Forbidden));
        }
    }

    private sealed class InMemorySeasonTeamRepository(
        SeasonTeam? seasonTeam = null)
        : ISeasonTeamRepository, IUnitOfWork
    {
        public SeasonTeam? SeasonTeam { get; } = seasonTeam;

        public int GetByIdCalls { get; private set; }

        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCalls++;
            return Task.FromResult(
                SeasonTeam?.SeasonTeamId == id ? SeasonTeam : null);
        }

        public Task<IReadOnlyList<SeasonTeam>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SeasonTeam>>(
                SeasonTeam is null ? [] : [SeasonTeam]);

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }
}
