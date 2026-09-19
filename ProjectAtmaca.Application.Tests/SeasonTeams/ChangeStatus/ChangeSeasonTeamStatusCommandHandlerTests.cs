using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.SeasonTeams;
using ProjectAtmaca.Application.SeasonTeams.ChangeStatus;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.Tests.SeasonTeams.ChangeStatus;

public sealed class ChangeSeasonTeamStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_ChangeStatus_AndPersist()
    {
        SeasonTeam team = CreateTeam();
        FakeUnitOfWork unitOfWork = new();
        var handler = new ChangeSeasonTeamStatusCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(team),
            unitOfWork);

        Result result = await handler.Handle(
            new ChangeSeasonTeamStatusCommand(team.SeasonTeamId, false),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        team.Status.Should().Be(SeasonTeamStatus.Inactive);
        unitOfWork.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTeamDoesNotExist()
    {
        var handler = new ChangeSeasonTeamStatusCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(null),
            new FakeUnitOfWork());

        Result result = await handler.Handle(
            new ChangeSeasonTeamStatusCommand(SeasonTeamId.New(), false),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(SeasonTeamApplicationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_NotAccessRepository_WhenPermissionIsDenied()
    {
        FakeRepository repository = new(CreateTeam());
        var handler = new ChangeSeasonTeamStatusCommandHandler(
            new DenyingAuthorization(),
            repository,
            new FakeUnitOfWork());

        Result result = await handler.Handle(
            new ChangeSeasonTeamStatusCommand(SeasonTeamId.New(), false),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetByIdCalls.Should().Be(0);
    }

    private static SeasonTeam CreateTeam() =>
        SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "U16").Value!;

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class DenyingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(ActorAuthorizationErrors.Forbidden));
    }

    private sealed class FakeRepository(SeasonTeam? team) : ISeasonTeamRepository
    {
        public int GetByIdCalls { get; private set; }

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCalls++;
            return Task.FromResult(team);
        }

        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<SeasonTeam>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SeasonTeam>>([]);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int Calls { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(1);
        }
    }
}
