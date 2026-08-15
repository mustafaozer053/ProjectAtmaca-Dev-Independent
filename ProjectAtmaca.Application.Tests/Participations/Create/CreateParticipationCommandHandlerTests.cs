using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests.Participations.Create;

public sealed class CreateParticipationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnFailure_AndNotPersist_WhenParticipationAlreadyExists()
    {
        // Arrange
        var repository = new FakeParticipationRepository
        {
            ExistsResult = true
        };

        var unitOfWork = new FakeUnitOfWork();

        var handler = new CreateParticipationCommandHandler(
            repository,
            unitOfWork);

        var command = new CreateParticipationCommand(
            ActivityReference.ForTraining(
            TrainingId.From(Guid.NewGuid())),
            AtmacaCardId.From(Guid.NewGuid()));

        // Act
        var result = await handler.Handle(
                            command,
                            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(
            CreateParticipationErrors.AlreadyExists);

        repository.AddCallCount.Should().Be(0);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }
    [Fact]

    public async Task Handle_Should_CreateAndPersistParticipation_WhenParticipationDoesNotExist()
    {
        // Arrange
        var repository = new FakeParticipationRepository
        {
            ExistsResult = false
        };

        var unitOfWork = new FakeUnitOfWork();

        var handler = new CreateParticipationCommandHandler(
            repository,
            unitOfWork);

        var activityReference =
            ActivityReference.ForTraining(
                TrainingId.From(Guid.NewGuid()));

        var atmacaCardId =
            AtmacaCardId.From(Guid.NewGuid());

        var command = new CreateParticipationCommand(
            activityReference,
            atmacaCardId);

        // Act
        var result = await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        repository.AddCallCount.Should().Be(1);
        repository.AddedParticipation.Should().NotBeNull();

        repository.AddedParticipation!.ActivityReference
            .Should()
            .Be(activityReference);

        repository.AddedParticipation.AtmacaCardId
            .Should()
            .Be(atmacaCardId);

        repository.AddedParticipation.ParticipationId
            .Should()
            .Be(result.Value);

        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }
    [Fact]
    public async Task Handle_Should_PropagateCancellationToken_ToRepositoryAndUnitOfWork()
    {
        // Arrange
        var repository = new FakeParticipationRepository
        {
            ExistsResult = false
        };

        var unitOfWork = new FakeUnitOfWork();

        var handler = new CreateParticipationCommandHandler(
            repository,
            unitOfWork);

        var command = new CreateParticipationCommand(
            ActivityReference.ForTraining(
                TrainingId.From(Guid.NewGuid())),
            AtmacaCardId.From(Guid.NewGuid()));

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        // Act
        var result = await handler.Handle(
            command,
            cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();

        repository.ExistsCancellationToken
            .Should()
            .Be(cancellationToken);

        repository.AddCancellationToken
            .Should()
            .Be(cancellationToken);

        unitOfWork.SaveChangesCancellationToken
            .Should()
            .Be(cancellationToken);
    }

    [Fact]
    public async Task Handle_Should_PropagateException_WhenPersistenceFails()
    {
        // Arrange
        var expectedException =
            new InvalidOperationException(
                "Simulated persistence failure.");

        var repository = new FakeParticipationRepository
        {
            ExistsResult = false
        };

        var unitOfWork = new FakeUnitOfWork
        {
            ExceptionToThrow = expectedException
        };

        var handler = new CreateParticipationCommandHandler(
            repository,
            unitOfWork);

        var command = new CreateParticipationCommand(
            ActivityReference.ForTraining(
                TrainingId.From(Guid.NewGuid())),
            AtmacaCardId.From(Guid.NewGuid()));

        // Act
        Func<Task> act = async () =>
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        var assertion = await act
            .Should()
            .ThrowAsync<InvalidOperationException>();

        assertion.Which
            .Should()
            .BeSameAs(expectedException);

        repository.AddCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(1);
    }
    private sealed class FakeParticipationRepository
        : IParticipationRepository
    {
        public bool ExistsResult { get; init; }

        public int AddCallCount { get; private set; }

        public Participation? AddedParticipation { get; private set; }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Participation?>(null);
        }

        public Task<bool> ExistsAsync(
                AtmacaCardId atmacaCardId,
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            ExistsCancellationToken =
                cancellationToken;

            return Task.FromResult(
                ExistsResult);
        }

        public Task AddAsync(
                Participation participation,
                CancellationToken cancellationToken = default)
        {
            AddCallCount++;

            AddedParticipation =
                participation;

            AddCancellationToken =
                cancellationToken;

            return Task.CompletedTask;
        }
        public CancellationToken ExistsCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken AddCancellationToken
        {
            get;
            private set;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
                CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            SaveChangesCancellationToken =
                cancellationToken;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(1);
        }
        public CancellationToken SaveChangesCancellationToken
        {
            get;
            private set;
        }
        public Exception? ExceptionToThrow { get; set; }
    }
}