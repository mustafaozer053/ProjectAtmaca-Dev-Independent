using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.MarkAbsent;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests
    .Participations.MarkAbsent;

public sealed class MarkParticipationAbsentCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_MarkBtaAbsent_AndPersist()
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
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                participation.ParticipationId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        participation.Status.Should().Be(ParticipationStatus.Absent);
        participation.Condition.Should().Be(ParticipationCondition.Bta);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndNotPersist_WhenParticipationDoesNotExist()
    {
        FakeParticipationRepository repository = new();
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            repository,
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
    public async Task Handle_Should_BeIdempotent_WhenBtaIsAlreadyRecorded()
    {
        Participation participation = CreateParticipation();
        participation.MarkAbsent(ParticipationCondition.Bta)
            .IsSuccess.Should().BeTrue();
        FakeUnitOfWork unitOfWork = new();
        MarkParticipationAbsentCommandHandler handler = new(
            new GrantedActorAuthorizationService(),
            new FakeParticipationRepository
            {
                ParticipationToReturn = participation
            },
            unitOfWork);

        Result result = await handler.Handle(
            new MarkParticipationAbsentCommand(
                participation.ParticipationId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
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
