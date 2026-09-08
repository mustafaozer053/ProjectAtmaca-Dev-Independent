using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.RecordDeparture;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests
    .Participations.RecordDeparture;

public sealed class RecordParticipationDepartureCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_RecordDeparture_AndPersist()
    {
        // Arrange
        Participation participation =
            CreateParticipationWithArrival();

        DateTimeOffset leftAt =
            participation.JoinedAt!.Value.AddHours(1);

        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new RecordParticipationDepartureCommandHandler(
                new GrantedActorAuthorizationService(),
                repository,
                unitOfWork);

        var command =
            new RecordParticipationDepartureCommand(
                participation.ParticipationId,
                leftAt);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        participation.LeftAt
            .Should()
            .Be(leftAt);

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
            new RecordParticipationDepartureCommandHandler(
                new GrantedActorAuthorizationService(),
                repository,
                unitOfWork);

        var command =
            new RecordParticipationDepartureCommand(
                ParticipationId.From(
                    Guid.NewGuid()),
                new DateTimeOffset(
                    2026,
                    8,
                    19,
                    11,
                    0,
                    0,
                    TimeSpan.Zero));

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
                RecordParticipationDepartureErrors
                    .NotFound);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }
    [Fact]
    public async Task Handle_Should_PropagateDomainFailure_AndNotPersist_WhenDepartureIsRejected()
    {
        // Arrange
        Result<Participation> creationResult =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New());

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        Participation participation =
            creationResult.Value!;

        DateTimeOffset leftAt =
            new(
                2026,
                8,
                19,
                11,
                0,
                0,
                TimeSpan.Zero);

        Result expectedDomainFailure =
            participation.RecordDeparture(
                leftAt);

        expectedDomainFailure.IsFailure
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
            new RecordParticipationDepartureCommandHandler(
                new GrantedActorAuthorizationService(),
                repository,
                unitOfWork);

        var command =
            new RecordParticipationDepartureCommand(
                participation.ParticipationId,
                leftAt);

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
            .Be(expectedDomainFailure.Error);

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
            CreateParticipationWithArrival();

        DateTimeOffset leftAt =
            participation.JoinedAt!.Value.AddHours(1);

        var repository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new RecordParticipationDepartureCommandHandler(
                new GrantedActorAuthorizationService(),
                repository,
                unitOfWork);

        var command =
            new RecordParticipationDepartureCommand(
                participation.ParticipationId,
                leftAt);

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

        repository.LastCancellationToken
            .Should()
            .Be(cancellationToken);

        unitOfWork.LastCancellationToken
            .Should()
            .Be(cancellationToken);
    }
    [Fact]
    public async Task Handle_Should_PropagatePersistenceException()
    {
        // Arrange
        Participation participation =
            CreateParticipationWithArrival();

        DateTimeOffset leftAt =
            participation.JoinedAt!.Value.AddHours(1);

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
            new RecordParticipationDepartureCommandHandler(
                new GrantedActorAuthorizationService(),
                repository,
                unitOfWork);

        var command =
            new RecordParticipationDepartureCommand(
                participation.ParticipationId,
                leftAt);

        // Act
        Func<Task> act =
            async () =>
                await handler.Handle(
                    command,
                    TestContext.Current.CancellationToken);

        // Assert
        var assertion =
            await act.Should()
                .ThrowAsync<InvalidOperationException>();

        assertion.Which
            .Should()
            .BeSameAs(expectedException);

        participation.LeftAt
            .Should()
            .Be(leftAt);

        repository.GetByIdCallCount
            .Should()
            .Be(1);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(1);
    }
    private static Participation
    CreateParticipationWithArrival()
    {
        Result<Participation> creationResult =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New());

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        Participation participation =
            creationResult.Value!;

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                19,
                10,
                0,
                0,
                TimeSpan.Zero);

        Result arrivalResult =
            participation.RecordArrival(
                joinedAt);

        arrivalResult.IsSuccess
            .Should()
            .BeTrue();

        return participation;
    }

    private sealed class GrantedActorAuthorizationService
        : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Result.Success());
        }
    }

    private sealed class FakeParticipationRepository
        : IParticipationRepository
    {
        public Participation?
            ParticipationToReturn
        { get; init; }

        public int GetByIdCallCount { get; private set; }

        public Task<Participation?> GetByIdAsync(
        ParticipationId participationId,
        CancellationToken cancellationToken =
        default)
        {
            GetByIdCallCount++;

            LastCancellationToken =
                cancellationToken;

            return Task.FromResult(
                ParticipationToReturn);
        }

        public Task<bool> ExistsAsync(
        AtmacaCardId atmacaCardId,
        ActivityReference activityReference,
        CancellationToken cancellationToken =
        default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken =
                default)
        {
            throw new NotSupportedException();
        }
        public CancellationToken LastCancellationToken
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
        CancellationToken cancellationToken =
        default)
        {
            SaveChangesCallCount++;

            LastCancellationToken =
                cancellationToken;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(1);
        }
        public CancellationToken LastCancellationToken
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
