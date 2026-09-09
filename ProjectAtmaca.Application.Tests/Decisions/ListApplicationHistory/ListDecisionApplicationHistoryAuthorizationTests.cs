using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Decisions;
using ProjectAtmaca.Application.Decisions
    .ListApplicationHistory;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Application.Tests.Decisions
    .ListApplicationHistory;

public sealed class
    ListDecisionApplicationHistoryAuthorizationTests
{
    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutReaderAccess_WhenActorLacksListApplicationHistoryPermission()
    {
        DenyingActorAuthorizationService
            authorizationService =
                new();

        var reader =
            new UnexpectedDecisionApplicationReader();

        var handler =
            new ListDecisionApplicationHistoryQueryHandler(
                authorizationService,
                reader);

        var query =
            new ListDecisionApplicationHistoryQuery(
                DecisionId.New());

        using CancellationTokenSource cancellationSource =
            new();

        Result<IReadOnlyList<DecisionApplicationHistoryItem>>
            result =
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
                Permissions.Decisions
                    .ListApplicationHistory);

        authorizationService.ObservedCancellationToken
            .Should()
            .Be(cancellationSource.Token);

        reader.WasCalled
            .Should()
            .BeFalse();
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

    private sealed class UnexpectedDecisionApplicationReader
        : IDecisionApplicationReader
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<DecisionApplicationHistoryItem>>
            ListHistoryByDecisionAsync(
                DecisionId decisionId,
                CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            throw new InvalidOperationException(
                "Authorization did not precede decision-application " +
                "history reader access.");
        }
    }
}
