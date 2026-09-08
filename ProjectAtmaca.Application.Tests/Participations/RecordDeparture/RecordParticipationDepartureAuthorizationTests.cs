using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.RecordDeparture;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Tests
    .Participations.RecordDeparture;

public sealed class RecordParticipationDepartureAuthorizationTests
{
    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutPersistenceAccess_WhenActorLacksRecordDeparturePermission()
    {
        DenyingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationRepository repository =
            new();

        TrackingUnitOfWork unitOfWork =
            new();

        RecordParticipationDepartureCommandHandler handler =
            new(
                authorizationService,
                repository,
                unitOfWork);

        RecordParticipationDepartureCommand command =
            new(
                ParticipationId.From(
                    Guid.NewGuid()),
                new DateTimeOffset(
                    2026,
                    9,
                    8,
                    11,
                    0,
                    0,
                    TimeSpan.Zero));

        using CancellationTokenSource cancellationSource =
            new();

        Result result =
            await handler.Handle(
                command,
                cancellationSource.Token);

        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .BeSameAs(
                ActorAuthorizationErrors.Forbidden);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Participations.RecordDeparture);

        authorizationService.ObservedCancellationToken
            .Should()
            .Be(cancellationSource.Token);

        repository.GetByIdCallCount
            .Should()
            .Be(0);

        repository.ExistsCallCount
            .Should()
            .Be(0);

        repository.AddCallCount
            .Should()
            .Be(0);

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
    }

    private sealed class DenyingActorAuthorizationService
        : IActorAuthorizationService
    {
        public int CallCount { get; private set; }

        public Permission? ObservedPermission
        {
            get;
            private set;
        }

        public CancellationToken
            ObservedCancellationToken
        {
            get;
            private set;
        }

        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ObservedPermission =
                permission;

            ObservedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                Result.Failure(
                    ActorAuthorizationErrors.Forbidden));
        }
    }

    private sealed class TrackingParticipationRepository
        : IParticipationRepository
    {
        public int GetByIdCallCount { get; private set; }

        public int ExistsCallCount { get; private set; }

        public int AddCallCount { get; private set; }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            return Task.FromResult<Participation?>(
                null);
        }

        public Task<bool> ExistsAsync(
            AtmacaCardId atmacaCardId,
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
        {
            ExistsCallCount++;

            return Task.FromResult(
                false);
        }

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class TrackingUnitOfWork
        : IUnitOfWork
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
