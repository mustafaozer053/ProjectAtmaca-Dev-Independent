using ProjectAtmaca.Application.Decisions.ListApplicationHistory;
using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Application.Decisions;

public interface IDecisionApplicationReader
{
    Task<IReadOnlyList<DecisionApplicationHistoryItem>>
        ListHistoryByDecisionAsync(
            DecisionId decisionId,
            CancellationToken cancellationToken = default);
}
