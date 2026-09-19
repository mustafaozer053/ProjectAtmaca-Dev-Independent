using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
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
        seasonTeam.AddMembership(
            endedCard,
            AssignmentPeriod.Create(
                today.AddDays(-10),
                today.AddDays(-1)).Value!)
            .IsSuccess.Should().BeTrue();

        var handler = new GetSeasonTeamByIdQueryHandler(
            new GrantedAuthorizationService(),
            new FakeSeasonTeamRepository(seasonTeam));

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
        result.Value.Memberships[1].AtmacaCardId
            .Should().Be(activeCard.Value);
        result.Value.Memberships[1].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTeamDoesNotExist()
    {
        var handler = new GetSeasonTeamByIdQueryHandler(
            new GrantedAuthorizationService(),
            new FakeSeasonTeamRepository(null));

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
            repository);

        Result<SeasonTeamDetails> result = await handler.Handle(
            new GetSeasonTeamByIdQuery(SeasonTeamId.New()),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetByIdCallCount.Should().Be(0);
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
}
