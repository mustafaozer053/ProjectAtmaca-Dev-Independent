using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.Create;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests.Trainings.Create;

public sealed class CreateTrainingCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_CreateAndPersistTraining()
    {
        var repository = new FakeTrainingRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTrainingCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            new FakeTrainingTypeRepository(),
            unitOfWork);

        var result = await handler.Handle(
            new CreateTrainingCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "U15 Antrenmanı",
                "Taktik çalışma",
                "Ana saha",
                new DateOnly(2026, 9, 19),
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                new[]
                {
                    new TrainingTypeAssignmentInput(Guid.NewGuid(), 60)
                }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        repository.AddedTraining.Should().NotBeNull();
        repository.AddedTraining!.TrainingTypeAssignments.Should().ContainSingle();
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_NotPersist_WhenDurationsDoNotMatchSchedule()
    {
        var repository = new FakeTrainingRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTrainingCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            new FakeTrainingTypeRepository(),
            unitOfWork);

        var result = await handler.Handle(
            new CreateTrainingCommand(
                Guid.NewGuid(), Guid.NewGuid(), "U15", null, "Saha",
                new DateOnly(2026, 9, 19),
                new TimeOnly(16, 0), new TimeOnly(17, 0),
                new[]
                {
                    new TrainingTypeAssignmentInput(Guid.NewGuid(), 30)
                }),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(TrainingErrors.TrainingTypeDurationMismatch);
        repository.AddedTraining.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_NotPersist_WhenTrainingTypeDoesNotExist()
    {
        var repository = new FakeTrainingRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTrainingCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            new FakeTrainingTypeRepository(exists: false),
            unitOfWork);

        var result = await handler.Handle(
            CreateCommand(),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(TrainingCreationErrors.TrainingTypeNotFound);
        repository.AddedTraining.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_NotPersist_WhenTrainingTypeIsInactive()
    {
        var repository = new FakeTrainingRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTrainingCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            new FakeTrainingTypeRepository(active: false),
            unitOfWork);

        var result = await handler.Handle(
            CreateCommand(),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(TrainingCreationErrors.TrainingTypeInactive);
        repository.AddedTraining.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static CreateTrainingCommand CreateCommand() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "U15",
            null,
            "Saha",
            new DateOnly(2026, 9, 19),
            new TimeOnly(16, 0),
            new TimeOnly(17, 0),
            new[]
            {
                new TrainingTypeAssignmentInput(Guid.NewGuid(), 60)
            });

    private sealed class GrantedAuthorizationService : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FakeTrainingRepository : ITrainingRepository
    {
        public Training? AddedTraining { get; private set; }

        public Task AddAsync(
            Training training,
            CancellationToken cancellationToken = default)
        {
            AddedTraining = training;
            return Task.CompletedTask;
        }

        public Task<Training?> GetByIdAsync(
            TrainingId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Training?>(null);
    }

    private sealed class FakeTrainingTypeRepository : ITrainingTypeRepository
    {
        private readonly TrainingType? _trainingType;

        public FakeTrainingTypeRepository(
            bool exists = true,
            bool active = true)
        {
            _trainingType =
                exists
                    ? TrainingType.Create(
                        TrainingTypeCode.Create("TACTIC").Value!,
                        TrainingTypeName.Create("Taktik").Value!,
                        TrainingTypeDescription.Create(null).Value!,
                        1).Value
                    : null;

            if (!active)
                _trainingType?.Deactivate();
        }

        public Task<TrainingType?> GetByIdAsync(
            TrainingTypeId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_trainingType);

        public Task AddAsync(
            TrainingType trainingType,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<TrainingType>> ListAsync(
            bool activeOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TrainingType>>(
                Array.Empty<TrainingType>());
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
