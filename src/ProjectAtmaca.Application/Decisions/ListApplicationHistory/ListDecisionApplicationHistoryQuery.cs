using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Application.Decisions.ListApplicationHistory;

public sealed record ListDecisionApplicationHistoryQuery(
    DecisionId DecisionId);
