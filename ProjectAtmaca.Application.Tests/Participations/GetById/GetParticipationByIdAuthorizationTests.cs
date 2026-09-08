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
    .Participations.GetById;

public sealed class GetParticipationByIdAuthorizationTests
{
    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutReaderAccess_WhenActorLacksGetByIdPermission()
    {
        DenyingActorAuthorizationService
            authorizationService =
                new();

        TrackingParticipationReader reader =
            new();

        GetParticipationByIdQueryHandler handler =
            new(
                authorizationService,
                reader);

        GetParticipationByIdQuery query =
            new(
                Guid.NewGuid());

        using CancellationTokenSource cancellationSource =
            new();

        Result<ParticipationDetails> result =
            await handler.Handle(
                query,
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
                Permissions.Participations.GetById);

        authorizationService.ObservedCancellationToken
            .Should()
            .Be(cancellationSource.Token);

        reader.GetByIdCallCount
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

    private sealed class TrackingParticipationReader
        : IParticipationReader
    {
        public int GetByIdCallCount { get; private set; }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            return Task.FromResult<ParticipationDetails?>(
                null);
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
            throw new NotSupportedException();
        }
    }
}
