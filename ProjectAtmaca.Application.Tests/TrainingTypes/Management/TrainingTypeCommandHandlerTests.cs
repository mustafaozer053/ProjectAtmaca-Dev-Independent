using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.TrainingTypes;
using ProjectAtmaca.Application.TrainingTypes.ChangeStatus;
using ProjectAtmaca.Application.TrainingTypes.Create;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.Tests.TrainingTypes.Management;

public sealed class TrainingTypeCommandHandlerTests
{
    [Fact]
    public async Task Create_Should_PersistActiveTrainingType()
    {
        FakeTrainingTypeRepository repository = new();
        FakeUnitOfWork unitOfWork = new();
        var handler = new CreateTrainingTypeCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            unitOfWork);

        Result<TrainingTypeId> result = await handler.Handle(
            new CreateTrainingTypeCommand(
                "TACTIC",
                "Taktik",
                "Topla çalışma",
                2),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        repository.Added.Should().NotBeNull();
        repository.Added!.IsActive.Should().BeTrue();
        repository.Added.Code.Value.Should().Be("TACTIC");
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_Should_ReturnValidationFailureWithoutPersistence()
    {
        FakeTrainingTypeRepository repository = new();
        FakeUnitOfWork unitOfWork = new();
        var handler = new CreateTrainingTypeCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            unitOfWork);

        Result<TrainingTypeId> result = await handler.Handle(
            new CreateTrainingTypeCommand(
                "not valid",
                "Taktik",
                null,
                1),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        repository.Added.Should().BeNull();
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ChangeStatus_Should_UpdateAndPersistStatus()
    {
        TrainingType trainingType = CreateTrainingType();
        FakeTrainingTypeRepository repository = new(trainingType);
        FakeUnitOfWork unitOfWork = new();
        var handler = new ChangeTrainingTypeStatusCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            unitOfWork);

        Result result = await handler.Handle(
            new ChangeTrainingTypeStatusCommand(
                trainingType.TrainingTypeId,
                false),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        trainingType.IsActive.Should().BeFalse();
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChangeStatus_Should_ReturnNotFoundWithoutPersistence()
    {
        FakeTrainingTypeRepository repository = new();
        FakeUnitOfWork unitOfWork = new();
        var handler = new ChangeTrainingTypeStatusCommandHandler(
            new GrantedAuthorizationService(),
            repository,
            unitOfWork);

        Result result = await handler.Handle(
            new ChangeTrainingTypeStatusCommand(
                TrainingTypeId.New(),
                false),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(TrainingTypeApplicationErrors.NotFound);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ChangeStatus_Should_ReturnForbiddenWithoutRepositoryAccess()
    {
        FakeTrainingTypeRepository repository = new(CreateTrainingType());
        FakeUnitOfWork unitOfWork = new();
        var handler = new ChangeTrainingTypeStatusCommandHandler(
            new DenyingAuthorizationService(),
            repository,
            unitOfWork);

        Result result = await handler.Handle(
            new ChangeTrainingTypeStatusCommand(
                TrainingTypeId.New(),
                false),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetByIdCallCount.Should().Be(0);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static TrainingType CreateTrainingType() =>
        TrainingType.Create(
            TrainingTypeCode.Create("TACTIC").Value!,
            TrainingTypeName.Create("Taktik").Value!,
            TrainingTypeDescription.Create(null).Value!,
            1)
        .Value!;

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
            Task.FromResult(Result.Failure(ActorAuthorizationErrors.Forbidden));
    }

    private sealed class FakeTrainingTypeRepository(
        TrainingType? trainingType = null)
        : ITrainingTypeRepository
    {
        public TrainingType? Added { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public Task AddAsync(
            TrainingType trainingType,
            CancellationToken cancellationToken = default)
        {
            Added = trainingType;
            return Task.CompletedTask;
        }

        public Task<TrainingType?> GetByIdAsync(
            TrainingTypeId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(trainingType);
        }

        public Task<IReadOnlyList<TrainingType>> ListAsync(
            bool activeOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TrainingType>>([]);
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
