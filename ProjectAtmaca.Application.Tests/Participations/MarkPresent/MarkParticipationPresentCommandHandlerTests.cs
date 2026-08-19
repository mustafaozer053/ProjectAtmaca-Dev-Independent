using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests
    .Participations.MarkPresent;

public sealed class MarkParticipationPresentCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_MarkParticipationPresent_AndPersist()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new MarkParticipationPresentCommandHandler(
                repository,
                unitOfWork);

        var command =
            new MarkParticipationPresentCommand(
                participation.ParticipationId,
                null);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();

        participation.Status
            .Should()
            .Be(ParticipationStatus.Present);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(1);
    }
    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndNotPersist_WhenParticipationDoesNotExist()
    {
        // Arrange
        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn = null
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new MarkParticipationPresentCommandHandler(
                repository,
                unitOfWork);

        var command =
            new MarkParticipationPresentCommand(
                ParticipationId.From(
                    Guid.NewGuid()),
                null);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                MarkParticipationPresentErrors.NotFound);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }
    [Fact]
    public async Task Handle_Should_PropagateDomainFailure_AndNotPersist_WhenMarkPresentIsRejected()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Result markAbsentResult =
            participation.MarkAbsent();

        markAbsentResult.IsSuccess
            .Should()
            .BeTrue();

        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new MarkParticipationPresentCommandHandler(
                repository,
                unitOfWork);

        var command =
            new MarkParticipationPresentCommand(
                participation.ParticipationId,
                null);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ParticipationErrors
                    .ClassificationCorrectionRequired);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Absent);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }
    [Fact]
    public async Task Handle_Should_PropagateCancellationToken_ToRepositoryAndUnitOfWork()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new MarkParticipationPresentCommandHandler(
                repository,
                unitOfWork);

        var command =
            new MarkParticipationPresentCommand(
                participation.ParticipationId,
                null);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        // Act
        var result =
            await handler.Handle(
                command,
                cancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        repository.GetByIdCancellationToken
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
        Participation participation =
            CreateParticipation();

        var expectedException =
            new InvalidOperationException(
                "Simulated persistence failure.");

        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var unitOfWork =
            new FakeUnitOfWork
            {
                ExceptionToThrow =
                    expectedException
            };

        var handler =
            new MarkParticipationPresentCommandHandler(
                repository,
                unitOfWork);

        var command =
            new MarkParticipationPresentCommand(
                participation.ParticipationId,
                null);

        // Act
        Func<Task> act = async () =>
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        var assertion =
            await act
                .Should()
                .ThrowAsync<InvalidOperationException>();

        assertion.Which
            .Should()
            .BeSameAs(expectedException);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(1);
    }
    private static Participation CreateParticipation()
    {
        Result<Participation> creationResult =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.From(
                        Guid.NewGuid())),
                AtmacaCardId.From(
                    Guid.NewGuid()));

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        return creationResult.Value!;
    }

    private sealed class FakeParticipationRepository
        : IParticipationRepository
    {
        public Participation? ParticipationToReturn
        {
            get;
            init;
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            GetByIdCancellationToken =
                cancellationToken;

            return Task.FromResult(
                ParticipationToReturn);
        }

        public Task<bool> ExistsAsync(
            AtmacaCardId atmacaCardId,
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public CancellationToken GetByIdCancellationToken
        {
            get;
            private set;
        }
    }

    private sealed class FakeUnitOfWork
        : IUnitOfWork
    {
        public int SaveChangesCallCount
        {
            get;
            private set;
        }

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
        public Exception? ExceptionToThrow
        {
            get;
            set;
        }
    }
}
