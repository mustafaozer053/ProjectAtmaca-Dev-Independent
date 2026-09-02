using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Application.Decisions.ListApplicationHistory;

public sealed record DecisionApplicationHistoryItem(
    DecisionApplicationId DecisionApplicationId,
    DecisionId DecisionId,
    DecisionTargetReference Target,
    DecisionRevision AppliedDecisionRevision,
    DateTimeOffset AppliedAtUtc);
