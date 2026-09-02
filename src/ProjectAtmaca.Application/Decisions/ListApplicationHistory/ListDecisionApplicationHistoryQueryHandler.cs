using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Decisions.ListApplicationHistory;

public sealed class ListDecisionApplicationHistoryQueryHandler
{
    private readonly IDecisionApplicationReader _reader;

    public ListDecisionApplicationHistoryQueryHandler(
        IDecisionApplicationReader reader)
    {
        _reader =
            reader;
    }

    public async Task<Result<IReadOnlyList<DecisionApplicationHistoryItem>>>
        Handle(
            ListDecisionApplicationHistoryQuery query,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DecisionApplicationHistoryItem> items =
            await _reader.ListHistoryByDecisionAsync(
                query.DecisionId,
                cancellationToken);

        return Result<IReadOnlyList<DecisionApplicationHistoryItem>>
            .Success(
                items);
    }
}
