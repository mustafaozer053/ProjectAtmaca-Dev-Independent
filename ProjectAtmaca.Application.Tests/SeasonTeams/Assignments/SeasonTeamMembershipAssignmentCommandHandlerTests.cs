using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.SeasonTeams;
using ProjectAtmaca.Application.SeasonTeams.AddMembershipAssignment;
using ProjectAtmaca.Application.SeasonTeams.EndMembershipAssignment;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.Tests.SeasonTeams.Assignments;

public sealed class SeasonTeamMembershipAssignmentCommandHandlerTests
{
    [Fact]
    public async Task Add_Should_AddAssignment_AndPersist()
    {
        SeasonTeam team = CreateTeamWithMembership(out SeasonTeamMembership membership);
        FakeUnitOfWork unitOfWork = new();
        var handler = new AddSeasonTeamMembershipAssignmentCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(team),
            unitOfWork);

        Result<SeasonTeamMembershipAssignmentId> result = await handler.Handle(
            new AddSeasonTeamMembershipAssignmentCommand(
                team.SeasonTeamId.Value,
                membership.SeasonTeamMembershipId.Value,
                SeasonTeamAssignmentKind.Role,
                Guid.NewGuid(),
                "Player",
                new DateTime(2026, 7, 1),
                null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        membership.Assignments.Should().ContainSingle(x =>
            x.DisplayNameSnapshot == "Player");
        unitOfWork.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Add_Should_ReturnForbiddenWithoutRepositoryAccess_WhenPermissionIsDenied()
    {
        SeasonTeam team = CreateTeamWithMembership(out _);
        FakeRepository repository = new(team);
        var handler = new AddSeasonTeamMembershipAssignmentCommandHandler(
            new DenyingAuthorization(),
            repository,
            new FakeUnitOfWork());

        Result<SeasonTeamMembershipAssignmentId> result = await handler.Handle(
            new AddSeasonTeamMembershipAssignmentCommand(
                team.SeasonTeamId.Value,
                Guid.NewGuid(),
                SeasonTeamAssignmentKind.Role,
                Guid.NewGuid(),
                "Player",
                new DateTime(2026, 7, 1),
                null),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetByIdCalls.Should().Be(0);
    }

    [Fact]
    public async Task End_Should_EndAssignment_AndPersist()
    {
        SeasonTeam team = CreateTeamWithMembership(out SeasonTeamMembership membership);
        SeasonTeamMembershipAssignment assignment = team.AddMembershipAssignment(
            membership.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Duty,
            Guid.NewGuid(),
            "Yardımcı Antrenör",
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!)
            .Value!;
        FakeUnitOfWork unitOfWork = new();
        var handler = new EndSeasonTeamMembershipAssignmentCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(team),
            unitOfWork);

        Result result = await handler.Handle(
            new EndSeasonTeamMembershipAssignmentCommand(
                team.SeasonTeamId.Value,
                membership.SeasonTeamMembershipId.Value,
                assignment.SeasonTeamMembershipAssignmentId.Value,
                new DateTime(2026, 7, 31)),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        assignment.Period.EndDate.Should().Be(new DateTime(2026, 7, 31));
        unitOfWork.Calls.Should().Be(1);
    }

    private static SeasonTeam CreateTeamWithMembership(
        out SeasonTeamMembership membership)
    {
        SeasonTeam team = SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "U16").Value!;

        membership = team.AddMembership(
            AtmacaCardId.New(),
            AssignmentPeriod.Create(new DateTime(2026, 7, 1)).Value!).Value!;

        return team;
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
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(ActorAuthorizationErrors.Forbidden));
    }

    private sealed class FakeRepository(SeasonTeam? team) : ISeasonTeamRepository
    {
        public int GetByIdCalls { get; private set; }

        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCalls++;
            return Task.FromResult(team);
        }

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
