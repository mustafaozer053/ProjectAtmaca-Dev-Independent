using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Application.SeasonTeams;
using ProjectAtmaca.Application.SeasonTeams.GetById;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.Tests.SeasonTeams.GetById;

public sealed class GetSeasonTeamByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnTeamAndOrderedMembershipDetails()
    {
        SeasonTeam seasonTeam = CreateSeasonTeam();
        DateTime today = DateTime.UtcNow.Date;
        AtmacaCardId activeCard = AtmacaCardId.New();
        AtmacaCardId endedCard = AtmacaCardId.New();

        seasonTeam.AddMembership(
            activeCard,
            AssignmentPeriod.Create(today).Value!)
            .IsSuccess.Should().BeTrue();
        SeasonTeamMembership activeMembership = seasonTeam.Memberships
            .Single(x => x.AtmacaCardId == activeCard);
        activeMembership.AddAssignment(
            SeasonTeamAssignmentKind.Role,
            Guid.NewGuid(),
            "Player",
            AssignmentPeriod.Create(today).Value!)
            .IsSuccess.Should().BeTrue();
        activeMembership.AddAssignment(
            SeasonTeamAssignmentKind.Role,
            Guid.NewGuid(),
            "Captain",
            AssignmentPeriod.Create(today).Value!)
            .IsSuccess.Should().BeTrue();
        seasonTeam.AddMembership(
            endedCard,
            AssignmentPeriod.Create(
                today.AddDays(-10),
                today.AddDays(-1)).Value!)
            .IsSuccess.Should().BeTrue();

        var handler = new GetSeasonTeamByIdQueryHandler(
            new GrantedAuthorizationService(),
            new FakeSeasonTeamRepository(seasonTeam),
            new FakeAtmacaCardReader(
                new AtmacaCardSummary(
                    activeCard.Value,
                    Guid.NewGuid(),
                    "Mert Yılmaz",
                    "000124",
                    true)),
            new FakeSeasonPeriodReader(new DateTime(2020, 1, 1), new DateTime(2099, 6, 30)));

        Result<SeasonTeamDetails> result = await handler.Handle(
            new GetSeasonTeamByIdQuery(seasonTeam.SeasonTeamId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(seasonTeam.SeasonTeamId.Value);
        result.Value.Name.Should().Be("ÇAYKUR RİZESPOR U16");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Memberships.Should().HaveCount(2);
        result.Value.Memberships[0].AtmacaCardId
            .Should().Be(endedCard.Value);
        result.Value.Memberships[0].IsActive.Should().BeFalse();
        result.Value.Memberships[0].Assignments.Should().BeEmpty();
        result.Value.Memberships[0].AtmacaCardDisplayName
            .Should().BeNull();
        result.Value.Memberships[1].AtmacaCardId
            .Should().Be(activeCard.Value);
        result.Value.Memberships[1].IsActive.Should().BeTrue();
        result.Value.Memberships[1].Assignments.Should().HaveCount(2);
        result.Value.Memberships[1].Assignments
            .Select(x => x.DisplayNameSnapshot)
            .Should().BeEquivalentTo("Captain", "Player");
        result.Value.Memberships[1].AtmacaCardDisplayName
            .Should().Be("Mert Yılmaz");
        result.Value.Memberships[1].AtmacaCardNumber
            .Should().Be("000124");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTeamDoesNotExist()
    {
        var handler = new GetSeasonTeamByIdQueryHandler(
            new GrantedAuthorizationService(),
            new FakeSeasonTeamRepository(null),
            new FakeAtmacaCardReader(),
            new FakeSeasonPeriodReader(new DateTime(2020, 1, 1), new DateTime(2099, 6, 30)));

        Result<SeasonTeamDetails> result = await handler.Handle(
            new GetSeasonTeamByIdQuery(SeasonTeamId.New()),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(SeasonTeamApplicationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutRepositoryAccess_WhenPermissionIsDenied()
    {
        FakeSeasonTeamRepository repository = new(CreateSeasonTeam());
        var handler = new GetSeasonTeamByIdQueryHandler(
            new DenyingAuthorizationService(),
            repository,
            new FakeAtmacaCardReader(),
            new FakeSeasonPeriodReader(new DateTime(2020, 1, 1), new DateTime(2099, 6, 30)));

        Result<SeasonTeamDetails> result = await handler.Handle(
            new GetSeasonTeamByIdQuery(SeasonTeamId.New()),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetByIdCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldEndMembershipAndAssignmentsAtSeasonEnd()
    {
        SeasonTeam team = CreateSeasonTeam();
        DateTime today = DateTime.UtcNow.Date;
        AtmacaCardId cardId = AtmacaCardId.New();
        team.AddMembership(cardId, AssignmentPeriod.Create(today.AddYears(-1)).Value!);
        SeasonTeamMembership membership = team.Memberships.Single();
        membership.AddAssignment(
            SeasonTeamAssignmentKind.Duty,
            Guid.NewGuid(),
            "Takım Teknik Sorumlusu",
            AssignmentPeriod.Create(today.AddMonths(-6)).Value!);
        var seasonEnd = today.AddDays(-1);
        var handler = new GetSeasonTeamByIdQueryHandler(
            new GrantedAuthorizationService(),
            new FakeSeasonTeamRepository(team),
            new FakeAtmacaCardReader(),
            new FakeSeasonPeriodReader(today.AddYears(-1), seasonEnd));

        Result<SeasonTeamDetails> result = await handler.Handle(
            new GetSeasonTeamByIdQuery(team.SeasonTeamId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        SeasonTeamMembershipDetails member = result.Value!.Memberships.Single();
        member.IsActive.Should().BeFalse();
        member.EndDate.Should().BeNull();
        member.Assignments.Should().ContainSingle();
        member.Assignments[0].IsActive.Should().BeFalse();
        member.Assignments[0].EndDate.Should().Be(seasonEnd);
    }

    private static SeasonTeam CreateSeasonTeam() =>
        SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "ÇAYKUR RİZESPOR U16")
        .Value!;

    private sealed class GrantedAuthorizationService : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class DenyingAuthorizationService
        : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(ActorAuthorizationErrors.Forbidden));
    }

    private sealed class FakeSeasonTeamRepository(SeasonTeam? seasonTeam)
        : ISeasonTeamRepository
    {
        public int GetByIdCallCount { get; private set; }

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(seasonTeam);
        }

        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<SeasonTeam>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SeasonTeam>>([]);
    }

    private sealed class FakeAtmacaCardReader
        : IAtmacaCardReader
    {
        private readonly IReadOnlyList<AtmacaCardSummary> _summaries;

        public FakeAtmacaCardReader(
            params AtmacaCardSummary[] summaries)
        {
            _summaries = summaries;
        }

        public Task<AtmacaCardSummary?> GetByPersonIdAsync(
            Guid personId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AtmacaCardSummary?>(_summaries.SingleOrDefault(
                summary => summary.PersonId == personId));

        public Task<IReadOnlyList<AtmacaCardSummary>> ListAsync(
            int limit = 100,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AtmacaCardSummary>>(
                _summaries.Take(limit).ToList());

        public Task<IReadOnlyDictionary<Guid, AtmacaCardSummary>> GetSummariesAsync(
            IReadOnlyCollection<Guid> atmacaCardIds,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, AtmacaCardSummary> result =
                _summaries.ToDictionary(x => x.AtmacaCardId, x => x);
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<AtmacaCardSummary>> SearchAsync(
            string search,
            int limit = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AtmacaCardSummary>>([]);
    }

    private sealed class FakeSeasonPeriodReader(DateTime startDate, DateTime endDate)
        : ISeasonPeriodReader
    {
        public Task<DateRange?> GetPeriodAsync(
            Guid seasonId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<DateRange?>(DateRange.Create(startDate, endDate));
    }
}
