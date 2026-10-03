using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.MarkAbsent;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.Tests
    .Participations.MarkAbsent;

public sealed class MarkParticipationAbsentCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_MarkOrdinaryAbsent_AndPersist()
    {
        Participation participation = CreateParticipation();
        FakeParticipationRepository repository = new()
        {
            ParticipationToReturn = participation
        };
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            repository,
            CreateBtaEligibilityValidator(),
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                participation.ParticipationId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        participation.Status.Should().Be(ParticipationStatus.Absent);
        participation.Condition.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_MarkBtaAbsent_WhenExplicitlyRequested()
    {
        Participation participation = CreateParticipation();
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            new FakeParticipationRepository
            {
                ParticipationToReturn = participation
            },
            CreateEligibleBtaEligibilityValidator(participation.AtmacaCardId),
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                participation.ParticipationId,
                IsBta: true),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        participation.Status.Should().Be(ParticipationStatus.Absent);
        participation.Condition.Should().Be(ParticipationCondition.Bta);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_RejectBta_WhenAthleteHasNoOtherActiveTeam()
    {
        Participation participation = CreateParticipation();
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            new FakeParticipationRepository
            {
                ParticipationToReturn = participation
            },
            CreateBtaEligibilityValidator(),
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                participation.ParticipationId,
                IsBta: true),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(BtaEligibilityErrors.RequiresOtherActiveTeam);
        participation.Status.Should().Be(ParticipationStatus.NotRecorded);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndNotPersist_WhenParticipationDoesNotExist()
    {
        FakeParticipationRepository repository = new();
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            repository,
            CreateBtaEligibilityValidator(),
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                ParticipationId.From(Guid.NewGuid())),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(MarkParticipationAbsentErrors.NotFound);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_PropagateDomainFailure_AndNotPersist_WhenParticipationIsPresent()
    {
        Participation participation = CreateParticipation();
        participation.MarkPresent().IsSuccess.Should().BeTrue();
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            new FakeParticipationRepository
            {
                ParticipationToReturn = participation
            },
            CreateBtaEligibilityValidator(),
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                participation.ParticipationId),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(
            ParticipationErrors.ClassificationCorrectionRequired);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_BeIdempotent_WhenOrdinaryAbsenceIsAlreadyRecorded()
    {
        Participation participation = CreateParticipation();
        participation.MarkAbsent().IsSuccess.Should().BeTrue();
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            new FakeParticipationRepository
            {
                ParticipationToReturn = participation
            },
            CreateBtaEligibilityValidator(),
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                participation.ParticipationId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        participation.Condition.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutPersistenceAccess_WhenPermissionIsDenied()
    {
        FakeParticipationRepository repository = new()
        {
            ParticipationToReturn = CreateParticipation()
        };
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new DenyingActorAuthorizationService(),
            repository,
            CreateBtaEligibilityValidator(),
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                repository.ParticipationToReturn!.ParticipationId),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetByIdCallCount.Should().Be(0);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static Participation CreateParticipation()
    {
        return Participation.Create(
                ActivityReference.ForTraining(TrainingId.New()),
                AtmacaCardId.New())
            .Value!;
    }

    private static BtaEligibilityValidator CreateBtaEligibilityValidator() =>
        new(new FakeTrainingRepository(null), new FakeSeasonTeamRepository(null, []));

    private static BtaEligibilityValidator CreateEligibleBtaEligibilityValidator(
        AtmacaCardId cardId)
    {
        SeasonId seasonId = SeasonId.New();
        SeasonTeam currentTeam = CreateSeasonTeam(seasonId, "Current team");
        SeasonTeam otherTeam = CreateSeasonTeam(seasonId, "Other team");
        var membership = otherTeam.AddMembership(
            cardId,
            AssignmentPeriod.Create(new DateTime(2026, 9, 1)).Value!);
        membership.IsSuccess.Should().BeTrue();
        otherTeam.AddMembershipAssignment(
            membership.Value!.SeasonTeamMembershipId,
            SeasonTeamAssignmentKind.Classification,
            Guid.NewGuid(),
            "Sporcu",
            AssignmentPeriod.Create(new DateTime(2026, 9, 1)).Value!)
            .IsSuccess.Should().BeTrue();

        Training training = CreateTraining(currentTeam.SeasonTeamId, seasonId);
        return new BtaEligibilityValidator(
            new FakeTrainingRepository(training),
            new FakeSeasonTeamRepository(
                currentTeam,
                [currentTeam, otherTeam]));
    }

    private static SeasonTeam CreateSeasonTeam(SeasonId seasonId, string name) =>
        SeasonTeam.Create(
            seasonId,
            OrganizationId.New(),
            Guid.NewGuid(),
            name).Value!;

    private static Training CreateTraining(
        SeasonTeamId teamId,
        SeasonId seasonId)
    {
        var context = SeasonOrganization.Create(seasonId, OrganizationId.New());
        var schedule = TrainingSchedule.Create(
            new DateOnly(2026, 10, 3),
            new TimeOnly(16, 0),
            new TimeOnly(17, 0)).Value!;
        var assignments = new[]
        {
            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                TrainingTypeDuration.Create(60).Value!)
        };

        return Training.Create(
            context,
            teamId,
            TrainingTitle.Create("Training").Value!,
            TrainingDescription.Create("Description").Value!,
            TrainingLocation.Create("Field").Value!,
            schedule,
            assignments).Value!;
    }

    private sealed class FakeTrainingRepository(Training? training) : ITrainingRepository
    {
        public Task AddAsync(Training item, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Training?> GetByIdAsync(
            TrainingId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(training);

        public Task<IReadOnlyList<Training>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Training>>([]);
    }

    private sealed class FakeSeasonTeamRepository(
        SeasonTeam? currentTeam,
        IReadOnlyList<SeasonTeam> teams) : ISeasonTeamRepository
    {
        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(currentTeam);

        public Task<IReadOnlyList<SeasonTeam>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(teams);
    }

    private sealed class GrantedActorAuthorizationService
        : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class DenyingActorAuthorizationService
        : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(
                    ActorAuthorizationErrors.Forbidden));
    }

    private sealed class FakeParticipationRepository
        : IParticipationRepository
    {
        public Participation? ParticipationToReturn { get; init; }

        public int GetByIdCallCount { get; private set; }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(ParticipationToReturn);
        }

        public Task<bool> ExistsAsync(
            AtmacaCardId atmacaCardId,
            ActivityReference activityReference,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}
