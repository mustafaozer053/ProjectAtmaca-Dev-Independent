using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.CreateForTraining;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.Tests
    .Participations.CreateForTraining;

public sealed class CreateTrainingParticipationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_CreateParticipation_WhenMembershipIsActiveOnTrainingDate()
    {
        SeasonTeam seasonTeam = CreateSeasonTeam();
        AtmacaCardId cardId = AtmacaCardId.New();
        seasonTeam.AddMembership(
            cardId,
            AssignmentPeriod.Create(new DateTime(2026, 9, 1)).Value!)
            .IsSuccess.Should().BeTrue();

        Training training = CreateTraining(seasonTeam.SeasonTeamId);
        FakeParticipationRepository participationRepository = new();
        FakeUnitOfWork unitOfWork = new();
        CreateTrainingParticipationCommandHandler handler = CreateHandler(
            training,
            seasonTeam,
            participationRepository,
            unitOfWork);

        Result<ParticipationId> result = await handler.Handle(
            new CreateTrainingParticipationCommand(
                training.TrainingId,
                cardId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        participationRepository.AddedParticipation.Should().NotBeNull();
        participationRepository.AddedParticipation!.ActivityReference
            .Should().Be(ActivityReference.ForTraining(training.TrainingId));
        participationRepository.AddedParticipation.AtmacaCardId
            .Should().Be(cardId);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnTrainingNotFound_AndNotPersist()
    {
        FakeParticipationRepository participationRepository = new();
        FakeUnitOfWork unitOfWork = new();
        CreateTrainingParticipationCommandHandler handler = CreateHandler(
            null,
            null,
            participationRepository,
            unitOfWork);

        Result<ParticipationId> result = await handler.Handle(
            new CreateTrainingParticipationCommand(
                TrainingId.New(),
                AtmacaCardId.New()),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(
            CreateTrainingParticipationErrors.TrainingNotFound);
        participationRepository.AddedParticipation.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReturnSeasonTeamRequired_WhenTrainingHasNoSeasonTeam()
    {
        Training training = CreateTraining(null);
        FakeParticipationRepository participationRepository = new();
        FakeUnitOfWork unitOfWork = new();
        CreateTrainingParticipationCommandHandler handler = CreateHandler(
            training,
            null,
            participationRepository,
            unitOfWork);

        Result<ParticipationId> result = await handler.Handle(
            new CreateTrainingParticipationCommand(
                training.TrainingId,
                AtmacaCardId.New()),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(
            CreateTrainingParticipationErrors.SeasonTeamRequired);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReturnMembershipNotActive_WhenMembershipDoesNotCoverTrainingDate()
    {
        SeasonTeam seasonTeam = CreateSeasonTeam();
        AtmacaCardId cardId = AtmacaCardId.New();
        seasonTeam.AddMembership(
            cardId,
            AssignmentPeriod.Create(
                new DateTime(2026, 10, 1)).Value!)
            .IsSuccess.Should().BeTrue();
        Training training = CreateTraining(seasonTeam.SeasonTeamId);
        FakeParticipationRepository participationRepository = new();
        FakeUnitOfWork unitOfWork = new();
        CreateTrainingParticipationCommandHandler handler = CreateHandler(
            training,
            seasonTeam,
            participationRepository,
            unitOfWork);

        Result<ParticipationId> result = await handler.Handle(
            new CreateTrainingParticipationCommand(
                training.TrainingId,
                cardId),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(
            CreateTrainingParticipationErrors.MembershipNotActive);
        participationRepository.AddedParticipation.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReturnSeasonTeamInactive_WhenTeamIsInactive()
    {
        SeasonTeam seasonTeam = CreateSeasonTeam();
        AtmacaCardId cardId = AtmacaCardId.New();
        seasonTeam.AddMembership(
            cardId,
            AssignmentPeriod.Create(new DateTime(2026, 9, 1)).Value!)
            .IsSuccess.Should().BeTrue();
        seasonTeam.Deactivate();
        Training training = CreateTraining(seasonTeam.SeasonTeamId);
        FakeParticipationRepository participationRepository = new();
        FakeUnitOfWork unitOfWork = new();
        CreateTrainingParticipationCommandHandler handler = CreateHandler(
            training,
            seasonTeam,
            participationRepository,
            unitOfWork);

        Result<ParticipationId> result = await handler.Handle(
            new CreateTrainingParticipationCommand(
                training.TrainingId,
                cardId),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(
            CreateTrainingParticipationErrors.SeasonTeamInactive);
        participationRepository.AddedParticipation.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyExists_AndNotPersist_WhenParticipationIsDuplicate()
    {
        SeasonTeam seasonTeam = CreateSeasonTeam();
        AtmacaCardId cardId = AtmacaCardId.New();
        seasonTeam.AddMembership(
            cardId,
            AssignmentPeriod.Create(new DateTime(2026, 9, 1)).Value!)
            .IsSuccess.Should().BeTrue();
        Training training = CreateTraining(seasonTeam.SeasonTeamId);
        FakeParticipationRepository participationRepository = new()
        {
            ParticipationExists = true
        };
        FakeUnitOfWork unitOfWork = new();
        CreateTrainingParticipationCommandHandler handler = CreateHandler(
            training,
            seasonTeam,
            participationRepository,
            unitOfWork);

        Result<ParticipationId> result = await handler.Handle(
            new CreateTrainingParticipationCommand(
                training.TrainingId,
                cardId),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(CreateParticipationErrors.AlreadyExists);
        participationRepository.AddedParticipation.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutRepositoryAccess_WhenPermissionIsDenied()
    {
        FakeTrainingRepository trainingRepository = new(null);
        FakeSeasonTeamRepository seasonTeamRepository = new(null);
        FakeParticipationRepository participationRepository = new();
        FakeUnitOfWork unitOfWork = new();
        CreateTrainingParticipationCommandHandler handler = new(
            new DenyingAuthorizationService(),
            participationRepository,
            trainingRepository,
            seasonTeamRepository,
            unitOfWork);

        Result<ParticipationId> result = await handler.Handle(
            new CreateTrainingParticipationCommand(
                TrainingId.New(),
                AtmacaCardId.New()),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        trainingRepository.GetByIdCallCount.Should().Be(0);
        seasonTeamRepository.GetByIdCallCount.Should().Be(0);
        participationRepository.ExistsCallCount.Should().Be(0);
        participationRepository.AddCallCount.Should().Be(0);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static CreateTrainingParticipationCommandHandler CreateHandler(
        Training? training,
        SeasonTeam? seasonTeam,
        FakeParticipationRepository participationRepository,
        FakeUnitOfWork unitOfWork) =>
        new(
            new GrantedAuthorizationService(),
            participationRepository,
            new FakeTrainingRepository(training),
            new FakeSeasonTeamRepository(seasonTeam),
            unitOfWork);

    private static SeasonTeam CreateSeasonTeam() =>
        SeasonTeam.Create(
            SeasonId.New(),
            OrganizationId.New(),
            Guid.NewGuid(),
            "ÇAYKUR RİZESPOR U16")
        .Value!;

    private static Training CreateTraining(SeasonTeamId? seasonTeamId)
    {
        SeasonId seasonId = SeasonId.New();
        OrganizationId organizationId = OrganizationId.New();
        SeasonOrganization context = SeasonOrganization.Create(
            seasonId,
            organizationId);
        TrainingSchedule schedule = TrainingSchedule.Create(
            new DateOnly(2026, 9, 19),
            new TimeOnly(16, 0),
            new TimeOnly(17, 0)).Value!;

        TrainingTitle title = TrainingTitle.Create("U16 Antrenmanı").Value!;
        TrainingDescription description =
            TrainingDescription.Create("Taktik çalışma").Value!;
        TrainingLocation location =
            TrainingLocation.Create("Ana saha").Value!;
        TrainingTypeAssignment[] assignments =
        [
            TrainingTypeAssignment.Create(
                TrainingTypeId.New(),
                TrainingTypeDuration.Create(60).Value!)
        ];

        return seasonTeamId is null
            ? Training.Create(
                context,
                title,
                description,
                location,
                schedule,
                assignments).Value!
            : Training.Create(
                context,
                seasonTeamId.Value,
                title,
                description,
                location,
                schedule,
                assignments).Value!;
    }

    private sealed class GrantedAuthorizationService : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class DenyingAuthorizationService : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(
                    ActorAuthorizationErrors.Forbidden));
    }

    private sealed class FakeTrainingRepository(Training? training)
        : ITrainingRepository
    {
        public int GetByIdCallCount { get; private set; }

        public Task AddAsync(
            Training training,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Training?> GetByIdAsync(
            TrainingId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(training);
        }

        public Task<IReadOnlyList<Training>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Training>>([]);
    }

    private sealed class FakeSeasonTeamRepository(SeasonTeam? seasonTeam)
        : ISeasonTeamRepository
    {
        public int GetByIdCallCount { get; private set; }

        public Task AddAsync(
            SeasonTeam seasonTeam,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SeasonTeam?> GetByIdAsync(
            SeasonTeamId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(seasonTeam);
        }

        public Task<IReadOnlyList<SeasonTeam>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SeasonTeam>>([]);
    }

    private sealed class FakeParticipationRepository
        : IParticipationRepository
    {
        public bool ParticipationExists { get; init; }

        public Participation? AddedParticipation { get; private set; }

        public int ExistsCallCount { get; private set; }

        public int AddCallCount { get; private set; }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Participation?>(null);

        public Task<bool> ExistsAsync(
            AtmacaCardId atmacaCardId,
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
        {
            ExistsCallCount++;
            return Task.FromResult(ParticipationExists);
        }

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            AddedParticipation = participation;
            return Task.CompletedTask;
        }
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
