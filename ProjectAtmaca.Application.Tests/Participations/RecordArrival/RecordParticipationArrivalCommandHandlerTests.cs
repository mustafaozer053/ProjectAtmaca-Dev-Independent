using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Participations.RecordArrival;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests
    .Participations.RecordArrival;

public sealed class RecordParticipationArrivalCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_RecordArrival_AndPersist()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                19,
                10,
                30,
                0,
                TimeSpan.Zero);

        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new RecordParticipationArrivalCommandHandler(
                repository,
                unitOfWork);

        var command =
            new RecordParticipationArrivalCommand(
                participation.ParticipationId,
                joinedAt);

        // Act
        Result result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        participation.JoinedAt
            .Should()
            .Be(joinedAt);

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
            new RecordParticipationArrivalCommandHandler(
                repository,
                unitOfWork);

        var command =
            new RecordParticipationArrivalCommand(
                ParticipationId.From(
                    Guid.NewGuid()),
                new DateTimeOffset(
                    2026,
                    8,
                    19,
                    10,
                    30,
                    0,
                    TimeSpan.Zero));

        // Act
        Result result =
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
                RecordParticipationArrivalErrors.NotFound);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }
    [Fact]
    public async Task Handle_Should_PropagateDomainFailure_AndNotPersist_WhenRecordArrivalIsRejected()
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
            new RecordParticipationArrivalCommandHandler(
                repository,
                unitOfWork);

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                19,
                10,
                30,
                0,
                TimeSpan.Zero);

        var command =
            new RecordParticipationArrivalCommand(
                participation.ParticipationId,
                joinedAt);

        // Act
        Result result =
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
                    .ArrivalCannotBeRecordedWhenAbsent);

        participation.JoinedAt
            .Should()
            .BeNull();

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }
    [Fact]
    public async Task Handle_Should_PropagateCancellationToken()
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
            new RecordParticipationArrivalCommandHandler(
                repository,
                unitOfWork);

        var command =
            new RecordParticipationArrivalCommand(
                participation.ParticipationId,
                new DateTimeOffset(
                    2026,
                    8,
                    19,
                    10,
                    30,
                    0,
                    TimeSpan.Zero));

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        // Act
        Result result =
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
    public async Task Handle_Should_PropagatePersistenceException()
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

        var expectedException =
            new InvalidOperationException(
                "Persistence failure.");

        var unitOfWork =
            new FakeUnitOfWork
            {
                ExceptionToThrow =
                    expectedException
            };

        var handler =
            new RecordParticipationArrivalCommandHandler(
                repository,
                unitOfWork);

        var command =
            new RecordParticipationArrivalCommand(
                participation.ParticipationId,
                new DateTimeOffset(
                    2026,
                    8,
                    19,
                    10,
                    30,
                    0,
                    TimeSpan.Zero));

        // Act
        Func<Task> act =
            () => handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        var assertion =
            await act.Should()
                .ThrowAsync<InvalidOperationException>();

        assertion.Which
            .Should()
            .BeSameAs(expectedException);

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
                    TrainingId.New()),
                AtmacaCardId.New());

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
            init;
        }
    }
}
