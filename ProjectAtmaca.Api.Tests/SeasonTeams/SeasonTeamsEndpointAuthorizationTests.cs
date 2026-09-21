using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Api.Tests.SeasonTeams;

public sealed class SeasonTeamsEndpointAuthorizationTests
{
    private static readonly Guid SeasonTeamId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid MembershipId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData("POST", "/api/season-teams")]
    [InlineData("GET", "/api/season-teams")]
    [InlineData("GET", "/api/season-teams/11111111-1111-1111-1111-111111111111")]
    [InlineData("GET", "/api/season-teams/11111111-1111-1111-1111-111111111111/roster-view")]
    [InlineData("PATCH", "/api/season-teams/11111111-1111-1111-1111-111111111111/status")]
    [InlineData("POST", "/api/season-teams/11111111-1111-1111-1111-111111111111/memberships")]
    [InlineData("POST", "/api/season-teams/11111111-1111-1111-1111-111111111111/memberships/22222222-2222-2222-2222-222222222222/end")]
    [InlineData("POST", "/api/season-teams/11111111-1111-1111-1111-111111111111/memberships/22222222-2222-2222-2222-222222222222/assignments")]
    [InlineData("POST", "/api/season-teams/11111111-1111-1111-1111-111111111111/memberships/22222222-2222-2222-2222-222222222222/assignments/33333333-3333-3333-3333-333333333333/end")]
    public async Task Endpoint_Should_ReturnForbidden_WhenPermissionIsDenied(
        string methodName,
        string path)
    {
        DenyingAuthorization authorization = new();
        using var factory = CreateFactory(authorization);
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = new(
            new HttpMethod(methodName),
            path);
        request.Content = methodName == "POST" && path == "/api/season-teams"
            ? JsonContent.Create(new
            {
                seasonId = Guid.NewGuid(),
                organizationId = Guid.NewGuid(),
                ageGroupId = Guid.NewGuid(),
                name = "U16"
            })
            : path.EndsWith("/status", StringComparison.Ordinal)
                ? JsonContent.Create(new { isActive = false })
                : path.EndsWith("/end", StringComparison.Ordinal)
                    ? JsonContent.Create(new { endDate = DateTime.UtcNow.Date })
                    : path.EndsWith("/assignments", StringComparison.Ordinal)
                        ? JsonContent.Create(new
                        {
                            kind = SeasonTeamAssignmentKind.Role,
                            definitionId = Guid.NewGuid(),
                            displayNameSnapshot = "Player",
                            startDate = DateTime.UtcNow.Date,
                            endDate = (DateTime?)null
                        })
                    : path.EndsWith("/memberships", StringComparison.Ordinal)
                        ? JsonContent.Create(new
                        {
                            atmacaCardId = Guid.NewGuid(),
                            startDate = DateTime.UtcNow.Date,
                            endDate = (DateTime?)null
                        })
                        : null;

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        authorization.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Endpoints_Should_ChallengeUnauthenticatedRequest()
    {
        using ProjectAtmacaApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/season-teams",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static WebApplicationFactory<global::Program> CreateFactory(
        DenyingAuthorization authorization)
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
                    services.AddSingleton<IActorAuthorizationService>(
                        authorization);
                    services.RemoveAll<ISeasonTeamRepository>();
                    services.AddSingleton<ISeasonTeamRepository>(
                        new UnexpectedRepository());
                    services.RemoveAll<IUnitOfWork>();
                    services.AddSingleton<IUnitOfWork>(
                        new UnexpectedUnitOfWork());
                }));
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
                Result.Failure(ActorAuthorizationErrors.Forbidden));
        }
    }

    private sealed class UnexpectedRepository : ISeasonTeamRepository
    {
        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected season team write.");

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected season team read.");

        public Task<IReadOnlyList<SeasonTeam>> ListAsync(
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected season team list.");
    }

    private sealed class UnexpectedUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected persistence.");
    }
}
