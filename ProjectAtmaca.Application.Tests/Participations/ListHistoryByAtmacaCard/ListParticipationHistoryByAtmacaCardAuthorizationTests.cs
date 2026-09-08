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

using Xunit;

namespace ProjectAtmaca.Application.Tests
    .Participations.ListHistoryByAtmacaCard;

public sealed class
    ListParticipationHistoryByAtmacaCardAuthorizationTests
{
    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutReaderAccess_WhenActorLacksListHistoryByAtmacaCardPermission()
    {
        // Arrange
        var authorizationService =
            new DenyingActorAuthorizationService();

        var reader =
            new TrackingParticipationReader();

        var handler =
            new ListParticipationHistoryByAtmacaCardQueryHandler(
                authorizationService,
                reader);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                AtmacaCardId.New(),
                PageSize: 25,
                Cursor: null);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        // Act
        Result<ParticipationHistoryPage> result =
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
                Permissions.Participations
                    .ListHistoryByAtmacaCard);

        authorizationService.ObservedCancellationToken
            .Should()
            .Be(cancellationToken);

        reader.ListHistoryByAtmacaCardCallCount
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
        public int ListHistoryByAtmacaCardCallCount
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
            throw new NotSupportedException();
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
            ListHistoryByAtmacaCardCallCount++;

            return Task.FromResult(
                new ParticipationHistoryPage(
                    Array.Empty<ParticipationHistoryItem>(),
                    NextCursor: null));
        }
    }
}