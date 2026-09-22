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
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Application.SeasonTeams;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Api.Tests.SeasonTeams;

public sealed class SeasonTeamsEndpointBehaviorTests
{
    [Fact]
    public async Task Post_Should_CreateSeasonTeam_AndReturnCreatedLocation()
    {
        InMemoryRepository repository = new();
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();
        Guid seasonId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Guid ageGroupId = Guid.NewGuid();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/season-teams",
            new
            {
                seasonId,
                organizationId,
                ageGroupId,
                name = "U16"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = await response.Content.ReadFromJsonAsync<Guid>(
            TestContext.Current.CancellationToken);
        response.Headers.Location!.ToString()
            .Should().Be($"/api/season-teams/{id:D}");
        repository.Values.Should().ContainSingle(x =>
            x.SeasonTeamId.Value == id &&
            x.SeasonId.Value == seasonId &&
            x.OrganizationId.Value == organizationId &&
            x.AgeGroupId == ageGroupId &&
            x.Name == "U16");
    }

    [Fact]
    public async Task Get_Should_ReturnOrderedSeasonTeams()
    {
        SeasonTeam earlier = CreateTeam("A Team");
        SeasonTeam later = CreateTeam("B Team");
        InMemoryRepository repository = new(earlier, later);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/season-teams",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<SeasonTeamResponse> values =
            await response.Content.ReadFromJsonAsync<List<SeasonTeamResponse>>(
                TestContext.Current.CancellationToken) ?? [];
        values.Select(x => x.Name).Should().Equal("A Team", "B Team");
    }

    [Fact]
    public async Task GetById_Should_ReturnMembershipDetails()
    {
        SeasonTeam team = CreateTeam("U16");
        DateTime startDate = DateTime.UtcNow.Date.AddDays(-1);
        SeasonTeamMembership membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(startDate).Value!).Value!;
        membership.AddAssignment(
            SeasonTeamAssignmentKind.Role,
            Guid.NewGuid(),
            "Player",
            AssignmentPeriod.Create(startDate).Value!)
            .IsSuccess.Should().BeTrue();
        InMemoryRepository repository = new(team);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/season-teams/{team.SeasonTeamId.Value:D}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("id").GetGuid()
            .Should().Be(team.SeasonTeamId.Value);
        body.RootElement.GetProperty("name").GetString().Should().Be("U16");
        body.RootElement.GetProperty("memberships").GetArrayLength()
            .Should().Be(1);
        body.RootElement.GetProperty("memberships")[0]
            .GetProperty("assignments")
            .GetArrayLength()
            .Should().Be(1);
    }

    [Fact]
    public async Task PostMembership_Should_AddMembership_AndReturnCreatedLocation()
    {
        SeasonTeam team = CreateTeam("U16");
        InMemoryRepository repository = new(team);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();
        Guid cardId = Guid.NewGuid();
        DateTime startDate = DateTime.UtcNow.Date;

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/season-teams/{team.SeasonTeamId.Value:D}/memberships",
            new
            {
                atmacaCardId = cardId,
                startDate,
                endDate = (DateTime?)null
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid membershipId = await response.Content.ReadFromJsonAsync<Guid>(
            TestContext.Current.CancellationToken);
        response.Headers.Location!.ToString()
            .Should().Be(
                $"/api/season-teams/{team.SeasonTeamId.Value:D}" +
                $"/memberships/{membershipId:D}");
        team.Memberships.Should().ContainSingle(x =>
            x.AtmacaCardId.Value == cardId);
    }

    [Fact]
    public async Task EndMembership_Should_CloseMembership_AndReturnNoContent()
    {
        SeasonTeam team = CreateTeam("U16");
        var membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!)
            .Value!;
        InMemoryRepository repository = new(team);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();
        DateTime endDate = DateTime.UtcNow.Date;

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/season-teams/{team.SeasonTeamId.Value:D}" +
            $"/memberships/{membership.SeasonTeamMembershipId.Value:D}/end",
            new { endDate },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        membership.Period.EndDate.Should().Be(endDate);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task PostMembershipAssignment_Should_AddAssignment_AndReturnCreatedLocation()
    {
        SeasonTeam team = CreateTeam("U16");
        SeasonTeamMembership membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!)
            .Value!;
        InMemoryRepository repository = new(team);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();
        Guid definitionId = Guid.NewGuid();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/season-teams/{team.SeasonTeamId.Value:D}/memberships/" +
            $"{membership.SeasonTeamMembershipId.Value:D}/assignments",
            new
            {
                kind = SeasonTeamAssignmentKind.Role,
                definitionId,
                displayNameSnapshot = "Captain",
                startDate = DateTime.UtcNow.Date,
                endDate = (DateTime?)null
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid assignmentId = await response.Content.ReadFromJsonAsync<Guid>(
            TestContext.Current.CancellationToken);
        response.Headers.Location!.ToString()
            .Should().Be(
                $"/api/season-teams/{team.SeasonTeamId.Value:D}/memberships/" +
                $"{membership.SeasonTeamMembershipId.Value:D}/assignments/{assignmentId:D}");
        membership.Assignments.Should().ContainSingle(x =>
            x.DefinitionId == definitionId &&
            x.DisplayNameSnapshot == "Captain");
    }

    [Fact]
    public async Task EndMembershipAssignment_Should_CloseAssignment_AndReturnNoContent()
    {
        SeasonTeam team = CreateTeam("U16");
        SeasonTeamMembership membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!)
            .Value!;
        SeasonTeamMembershipAssignment assignment = membership.AddAssignment(
            SeasonTeamAssignmentKind.Role,
            Guid.NewGuid(),
            "Captain",
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-1)).Value!)
            .Value!;
        InMemoryRepository repository = new(team);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();
        DateTime endDate = DateTime.UtcNow.Date;

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/season-teams/{team.SeasonTeamId.Value:D}/memberships/" +
            $"{membership.SeasonTeamMembershipId.Value:D}/assignments/" +
            $"{assignment.SeasonTeamMembershipAssignmentId.Value:D}/end",
            new { endDate },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        assignment.Period.EndDate.Should().Be(endDate);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetRosterView_Should_GroupMembershipsByActiveClassification()
    {
        SeasonTeam team = CreateTeam("U16");
        SeasonTeamMembership player = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!)
            .Value!;
        player.AddAssignment(
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Sporcu",
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!);

        SeasonTeamMembership technical = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!)
            .Value!;
        technical.AddAssignment(
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Teknik Ekip",
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!);

        SeasonTeamMembership dual = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!)
            .Value!;
        dual.AddAssignment(
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Sporcu",
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!);
        dual.AddAssignment(
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Teknik Ekip",
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!);

        SeasonTeamMembership unclassified = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!)
            .Value!;
        unclassified.AddAssignment(
            SeasonTeamAssignmentKind.Role,
            Guid.NewGuid(),
            "Captain",
            AssignmentPeriod.Create(DateTime.UtcNow.Date.AddDays(-2)).Value!);

        InMemoryRepository repository = new(team);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/season-teams/{team.SeasonTeamId.Value:D}/roster-view",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));

        JsonElement groups = body.RootElement.GetProperty("groups");
        groups.GetArrayLength().Should().Be(2);
        groups.EnumerateArray()
            .Single(x => x.GetProperty("classification").GetString() == "Sporcu")
            .GetProperty("memberships")
            .GetArrayLength()
            .Should().Be(2);
        groups.EnumerateArray()
            .Single(x => x.GetProperty("classification").GetString() == "Teknik Ekip")
            .GetProperty("memberships")
            .GetArrayLength()
            .Should().Be(2);

        body.RootElement.GetProperty("unclassifiedMemberships")
            .GetArrayLength()
            .Should().Be(1);
        body.RootElement.GetProperty("totalMembershipCount")
            .GetInt32()
            .Should().Be(4);
        body.RootElement.GetProperty("activeMembershipCount")
            .GetInt32()
            .Should().Be(4);
        body.RootElement.GetProperty("inactiveMembershipCount")
            .GetInt32()
            .Should().Be(0);
        body.RootElement.GetProperty("unclassifiedMembershipCount")
            .GetInt32()
            .Should().Be(1);
        JsonElement classificationCounts =
            body.RootElement.GetProperty("classificationCounts");
        classificationCounts.GetArrayLength().Should().Be(2);
        classificationCounts.EnumerateArray()
            .Single(x => x.GetProperty("classification").GetString() == "Sporcu")
            .GetProperty("membershipCount")
            .GetInt32()
            .Should().Be(2);
        classificationCounts.EnumerateArray()
            .Single(x => x.GetProperty("classification").GetString() == "Teknik Ekip")
            .GetProperty("membershipCount")
            .GetInt32()
            .Should().Be(2);
        JsonElement primarySections =
            body.RootElement.GetProperty("primaryRosterSections");
        primarySections.GetArrayLength().Should().Be(2);
        primarySections.EnumerateArray()
            .Single(x => x.GetProperty("sectionKey").GetString() == "players")
            .GetProperty("membershipCount")
            .GetInt32()
            .Should().Be(2);
        primarySections.EnumerateArray()
            .Single(x => x.GetProperty("sectionKey").GetString() == "technical-staff")
            .GetProperty("membershipCount")
            .GetInt32()
            .Should().Be(2);
    }

    private static WebApplicationFactory<global::Program> CreateFactory(
        InMemoryRepository repository)
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
                        new AllowingAuthorization());
                    services.RemoveAll<ISeasonTeamRepository>();
                    services.AddSingleton<ISeasonTeamRepository>(repository);
                    services.RemoveAll<IUnitOfWork>();
                    services.AddSingleton<IUnitOfWork>(repository);
                    services.RemoveAll<IAtmacaCardReader>();
                    services.AddSingleton<IAtmacaCardReader>(
                        new EmptyAtmacaCardReader());
                }));
    }

    private static SeasonTeam CreateTeam(string name) =>
        SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            name).Value!;

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

    private sealed class EmptyAtmacaCardReader : IAtmacaCardReader
    {
        public Task<IReadOnlyList<AtmacaCardSummary>> ListAsync(
            int limit = 100,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AtmacaCardSummary>>([]);

        public Task<IReadOnlyDictionary<Guid, AtmacaCardSummary>> GetSummariesAsync(
            IReadOnlyCollection<Guid> atmacaCardIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, AtmacaCardSummary>>(
                new Dictionary<Guid, AtmacaCardSummary>());

        public Task<IReadOnlyList<AtmacaCardSummary>> SearchAsync(
            string search,
            int limit = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AtmacaCardSummary>>([]);
    }

    private sealed class InMemoryRepository(params SeasonTeam[] values)
        : ISeasonTeamRepository, IUnitOfWork
    {
        public List<SeasonTeam> Values { get; } = [.. values];

        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default)
        {
            Values.Add(seasonTeam);
            return Task.CompletedTask;
        }

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Values.SingleOrDefault(x => x.SeasonTeamId == id));

        public Task<IReadOnlyList<SeasonTeam>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SeasonTeam>>(
                Values.OrderBy(x => x.Name).ToList());

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed record SeasonTeamResponse(
        Guid Id,
        Guid SeasonId,
        Guid OrganizationId,
        Guid AgeGroupId,
        string Name,
        int MembershipCount,
        bool IsActive);
}
