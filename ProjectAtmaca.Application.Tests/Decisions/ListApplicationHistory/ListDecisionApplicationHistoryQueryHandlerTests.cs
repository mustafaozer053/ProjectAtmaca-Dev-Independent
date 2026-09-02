using FluentAssertions;
using ProjectAtmaca.Application.Decisions;
using ProjectAtmaca.Application.Decisions.ListApplicationHistory;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests
    .Decisions.ListApplicationHistory;

public sealed class ListDecisionApplicationHistoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnHistoricalApplications_ForExactDecision()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        IReadOnlyList<DecisionApplicationHistoryItem> expected =
            new[]
            {
                new DecisionApplicationHistoryItem(
                    DecisionApplicationId.New(),
                    decisionId,
                    DecisionTargetReference.ForParticipation(
                        ParticipationId.New()),
                    DecisionRevision.From(2),
                    new DateTimeOffset(
                        2026,
                        9,
                        2,
                        7,
                        30,
                        0,
                        TimeSpan.Zero)),
                new DecisionApplicationHistoryItem(
                    DecisionApplicationId.New(),
                    decisionId,
                    DecisionTargetReference.ForParticipation(
                        ParticipationId.New()),
                    DecisionRevision.Initial,
                    new DateTimeOffset(
                        2026,
                        9,
                        1,
                        8,
                        15,
                        0,
                        TimeSpan.Zero))
            };

        var reader =
            new CapturingDecisionApplicationReader(
                expected);

        var handler =
            new ListDecisionApplicationHistoryQueryHandler(
                reader);

        var query =
            new ListDecisionApplicationHistoryQuery(
                decisionId);

        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;

        // Act
        Result<IReadOnlyList<DecisionApplicationHistoryItem>>
            result =
                await handler.Handle(
                    query,
                    cancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .BeSameAs(expected);

        reader.WasCalled
            .Should()
            .BeTrue();

        reader.CapturedDecisionId
            .Should()
            .Be(decisionId);

        reader.CapturedCancellationToken
            .Should()
            .Be(cancellationToken);
    }

    private sealed class CapturingDecisionApplicationReader
        : IDecisionApplicationReader
    {
        private readonly IReadOnlyList<
            DecisionApplicationHistoryItem> _items;

        public CapturingDecisionApplicationReader(
            IReadOnlyList<DecisionApplicationHistoryItem> items)
        {
            _items =
                items;
        }

        public bool WasCalled
        {
            get;
            private set;
        }

        public DecisionId? CapturedDecisionId
        {
            get;
            private set;
        }

        public CancellationToken CapturedCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<DecisionApplicationHistoryItem>>
            ListHistoryByDecisionAsync(
                DecisionId decisionId,
                CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            CapturedDecisionId =
                decisionId;

            CapturedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                _items);
        }
    }
}
