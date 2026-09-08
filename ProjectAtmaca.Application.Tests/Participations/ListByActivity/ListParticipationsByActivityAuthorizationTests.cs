using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

using Xunit;

namespace ProjectAtmaca.Application.Tests
    .Participations.ListByActivity;

public sealed class ListParticipationsByActivityAuthorizationTests
{
    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutReaderAccess_WhenActorLacksListByActivityPermission()
    {
        // Arrange
        var authorizationService =
            new DenyingActorAuthorizationService();

        var reader =
            new TrackingParticipationReader();

        var handler =
            new ListParticipationsByActivityQueryHandler(
                authorizationService,
                reader);

        var query =
            new ListParticipationsByActivityQuery(
                ActivityReference.ForTraining(
                    TrainingId.New()));

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        // Act
        Result<IReadOnlyList<ParticipationListItem>> result =
            await handler.Handle(
                query,
                cancellationToken);

        // Assert
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
                Permissions.Participations.ListByActivity);

        authorizationService.ObservedCancellationToken
            .Should()
            .Be(cancellationToken);

        reader.ListByActivityCallCount
            .Should()
            .Be(0);
    }

    private sealed class DenyingActorAuthorizationService
        : IActorAuthorizationService
    {
        public int CallCount
        {
            get;
            private set;
        }

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

    private sealed class TrackingParticipationReader
        : IParticipationReader
    {
        public int ListByActivityCallCount
        {
            get;
            private set;
        }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ParticipationListItem>>
            ListByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            ListByActivityCallCount++;

            return Task.FromResult<
                IReadOnlyList<ParticipationListItem>>(
                    Array.Empty<ParticipationListItem>());
        }

        public Task<ParticipationActivitySummary>
            GetSummaryByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ParticipationHistoryPage>
            ListHistoryByAtmacaCardAsync(
                AtmacaCardId atmacaCardId,
                int pageSize,
                ParticipationHistoryCursor? cursor,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}