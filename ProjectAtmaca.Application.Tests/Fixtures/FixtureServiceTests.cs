using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Fixtures;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Fixtures;
using ProjectAtmaca.Domain.SeasonTeams;
using Xunit;

namespace ProjectAtmaca.Application.Tests.Fixtures;

public sealed class FixtureServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistFixtureForActiveSeasonTeam()
    {
        var team = SeasonTeam.Create(
            ProjectAtmaca.Domain.Seasons.SeasonId.New(),
            ProjectAtmaca.Domain.Organizations.OrganizationId.New(),
            Guid.NewGuid(),
            "U15").Value!;
        var repository = new FakeFixtureRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new FixtureService(
            new GrantedAuthorization(),
            new FakeCurrentActor(),
            repository,
            new FakeSeasonTeamRepository(team),
            unitOfWork);

        var result = await service.CreateAsync(new CreateFixtureCommand(
            team.SeasonTeamId.Value,
            FixtureType.Official,
            "Rakip",
            new DateOnly(2026, 10, 4),
            new TimeOnly(15, 30),
            "Ana saha",
            FixtureVenueSide.Home,
            null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        repository.Items.Should().ContainSingle();
        unitOfWork.SaveCalls.Should().Be(1);
        repository.Items[0].SeasonTeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectEmptyTeamIdWithoutQueryingRepository()
    {
        var teamRepository = new FakeSeasonTeamRepository(null);
        var fixtureRepository = new FakeFixtureRepository();
        var service = new FixtureService(
            new GrantedAuthorization(),
            new FakeCurrentActor(),
            fixtureRepository,
            teamRepository,
            new FakeUnitOfWork());

        var result = await service.CreateAsync(new CreateFixtureCommand(
            Guid.Empty,
            FixtureType.Friendly,
            "Rakip",
            new DateOnly(2026, 10, 4),
            new TimeOnly(15, 30),
            "Ana saha",
            FixtureVenueSide.Away,
            null),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(FixtureErrors.SeasonTeamRequired);
        teamRepository.GetCalls.Should().Be(0);
        fixtureRepository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelAsync_ShouldPersistCancelledStatus()
    {
        var fixture = Fixture.Create(
            Guid.NewGuid(), FixtureType.Official, "Rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;
        var repository = new FakeFixtureRepository();
        repository.Items.Add(fixture);
        var unitOfWork = new FakeUnitOfWork();
        var service = new FixtureService(
            new GrantedAuthorization(),
            new FakeCurrentActor(),
            repository,
            new FakeSeasonTeamRepository(null), unitOfWork);

        var result = await service.CancelAsync(
            fixture.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        fixture.Status.Should().Be(FixtureStatus.Cancelled);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSaveChangedOpponentAndTime()
    {
        var team = SeasonTeam.Create(
            ProjectAtmaca.Domain.Seasons.SeasonId.New(),
            ProjectAtmaca.Domain.Organizations.OrganizationId.New(),
            Guid.NewGuid(),
            "U15").Value!;
        var fixture = Fixture.Create(
            team.Id, FixtureType.Official, "Eski rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;
        var repository = new FakeFixtureRepository();
        repository.Items.Add(fixture);
        var unitOfWork = new FakeUnitOfWork();
        var service = new FixtureService(
            new GrantedAuthorization(),
            new FakeCurrentActor(),
            repository,
            new FakeSeasonTeamRepository(team), unitOfWork);

        var result = await service.UpdateAsync(
            fixture.Id,
            new CreateFixtureCommand(
                team.Id, FixtureType.Friendly, "Yeni rakip",
                new DateOnly(2026, 10, 5), new TimeOnly(16, 0),
                "Deplasman sahası", FixtureVenueSide.Away, "Güncellendi"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        fixture.Opponent.Should().Be("Yeni rakip");
        fixture.Date.Should().Be(new DateOnly(2026, 10, 5));
        fixture.StartTime.Should().Be(new TimeOnly(16, 0));
        fixture.Status.Should().Be(FixtureStatus.Scheduled);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task UpdateMatchDetailsAsync_ShouldRejectPlayerOutsideEligibleTeamRoster()
    {
        var team = SeasonTeam.Create(
            ProjectAtmaca.Domain.Seasons.SeasonId.New(),
            ProjectAtmaca.Domain.Organizations.OrganizationId.New(),
            Guid.NewGuid(),
            "U15").Value!;
        var start = new DateTime(2026, 1, 1);
        var period = AssignmentPeriod.Create(start).Value!;
        var member = team.AddMembership(
            AtmacaCardId.New(), period).Value!;
        team.AddMembershipAssignment(
            member.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Sporcu",
            period).IsSuccess.Should().BeTrue();

        var fixture = Fixture.Create(
            team.Id, FixtureType.Official, "Rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;
        var fixtureRepository = new FakeFixtureRepository();
        fixtureRepository.Items.Add(fixture);
        var unitOfWork = new FakeUnitOfWork();
        var service = new FixtureService(
            new GrantedAuthorization(),
            new FakeCurrentActor(),
            fixtureRepository,
            new FakeSeasonTeamRepository(team),
            unitOfWork);

        var result = await service.UpdateMatchDetailsAsync(
            fixture.Id,
            40,
            null,
            null,
            [new FixtureSquadMemberInput(
                Guid.NewGuid(), FixtureSquadRole.Starter)],
            [],
            [],
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(FixtureErrors.SquadMemberNotEligible);
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateMatchDetailsAsync_ShouldPersistAttributedScoreForEligiblePlayer()
    {
        var team = SeasonTeam.Create(
            ProjectAtmaca.Domain.Seasons.SeasonId.New(),
            ProjectAtmaca.Domain.Organizations.OrganizationId.New(),
            Guid.NewGuid(),
            "U15").Value!;
        var period = AssignmentPeriod.Create(
            new DateTime(2026, 1, 1)).Value!;
        var cardId = AtmacaCardId.New();
        var member = team.AddMembership(cardId, period).Value!;
        team.AddMembershipAssignment(
            member.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Sporcu",
            period).IsSuccess.Should().BeTrue();
        var fixture = Fixture.Create(
            team.Id, FixtureType.Official, "Rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;
        var fixtureRepository = new FakeFixtureRepository();
        fixtureRepository.Items.Add(fixture);
        var service = new FixtureService(
            new GrantedAuthorization(),
            new FakeCurrentActor(),
            fixtureRepository,
            new FakeSeasonTeamRepository(team),
            new FakeUnitOfWork());

        var result = await service.UpdateMatchDetailsAsync(
            fixture.Id,
            40,
            "Hakem",
            null,
            [new FixtureSquadMemberInput(
                cardId.Value, FixtureSquadRole.Starter)],
            [],
            [new FixtureScoreEventInput(
                FixtureScoreSide.SeasonTeam,
                "FOOTBALL_GOAL",
                1,
                17,
                cardId.Value)],
            TestContext.Current.CancellationToken);
        var details = await service.GetDetailsAsync(
            fixture.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        details.Value!.OurScore.Should().Be(1);
        details.Value.ScoreEvents.Should().ContainSingle()
            .Which.AtmacaCardId.Should().Be(cardId.Value);
    }

    [Fact]
    public async Task UpdateMatchDetailsAsync_ShouldPreservePreviouslyRecordedSquadAfterBackdatedMembershipEnd()
    {
        var team = SeasonTeam.Create(
            ProjectAtmaca.Domain.Seasons.SeasonId.New(),
            ProjectAtmaca.Domain.Organizations.OrganizationId.New(),
            Guid.NewGuid(),
            "U15").Value!;
        var period = AssignmentPeriod.Create(
            new DateTime(2026, 1, 1)).Value!;
        var cardId = AtmacaCardId.New();
        var membership = team.AddMembership(cardId, period).Value!;
        var assignment = team.AddMembershipAssignment(
            membership.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Sporcu",
            period).Value!;
        var fixture = Fixture.Create(
            team.Id, FixtureType.Official, "Rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;
        var squad = new FixtureSquadMemberInput(
            cardId.Value, FixtureSquadRole.Starter);
        fixture.UpdateMatchDetails(90, null, null, [squad], [], []);
        team.EndMembership(
            membership.SeasonTeamMembershipId,
            new DateTime(2026, 9, 30)).IsSuccess.Should().BeTrue();
        team.EndMembershipAssignment(
            membership.SeasonTeamMembershipId,
            assignment.SeasonTeamMembershipAssignmentId,
            new DateTime(2026, 9, 30)).IsSuccess.Should().BeTrue();

        var repository = new FakeFixtureRepository();
        repository.Items.Add(fixture);
        var unitOfWork = new FakeUnitOfWork();
        var service = new FixtureService(
            new GrantedAuthorization(),
            new FakeCurrentActor(),
            repository,
            new FakeSeasonTeamRepository(team),
            unitOfWork);

        var result = await service.UpdateMatchDetailsAsync(
            fixture.Id, 90, null, null, [squad], [], [],
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        fixture.SquadMembers.Should().ContainSingle()
            .Which.AtmacaCardId.Should().Be(cardId.Value);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task ReopenForCorrectionAsync_ShouldPersistReasonActorAndScheduledStatus()
    {
        var fixture = Fixture.Create(
            Guid.NewGuid(), FixtureType.Official, "Rakip",
            new DateOnly(2026, 10, 4), new TimeOnly(15, 30),
            "Ana saha", FixtureVenueSide.Home, null).Value!;
        fixture.UpdateMatchDetails(90, null, null, [], [], []);
        fixture.Complete();
        var repository = new FakeFixtureRepository();
        repository.Items.Add(fixture);
        var actor = new FakeCurrentActor();
        var unitOfWork = new FakeUnitOfWork();
        var service = new FixtureService(
            new GrantedAuthorization(),
            actor,
            repository,
            new FakeSeasonTeamRepository(null),
            unitOfWork);

        var result = await service.ReopenForCorrectionAsync(
            fixture.Id, "Yanlış skor girildi",
            TestContext.Current.CancellationToken);
        var details = await service.GetDetailsAsync(
            fixture.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        fixture.Status.Should().Be(FixtureStatus.CorrectionInProgress);
        details.Value!.Corrections.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new FixtureCorrectionDetails(
                "Yanlış skor girildi",
                details.Value.Corrections[0].ReopenedAtUtc,
                actor.ActorId.Value));
        unitOfWork.SaveCalls.Should().Be(1);
    }

    private sealed class GrantedAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FakeCurrentActor : ICurrentActor
    {
        public ActorId ActorId { get; } = ActorId.New();
    }

    private sealed class FakeSeasonTeamRepository(SeasonTeam? team) : ISeasonTeamRepository
    {
        public int GetCalls { get; private set; }
        public Task AddAsync(SeasonTeam seasonTeam, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SeasonTeam?> GetByIdAsync(SeasonTeamId id, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(team);
        }
        public Task<IReadOnlyList<SeasonTeam>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SeasonTeam>>(team is null ? [] : [team]);
    }

    private sealed class FakeFixtureRepository : IFixtureRepository
    {
        public List<Fixture> Items { get; } = [];
        public Task AddAsync(Fixture fixture, CancellationToken cancellationToken = default)
        {
            Items.Add(fixture);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Fixture>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Fixture>>(Items);
        public Task<Fixture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
