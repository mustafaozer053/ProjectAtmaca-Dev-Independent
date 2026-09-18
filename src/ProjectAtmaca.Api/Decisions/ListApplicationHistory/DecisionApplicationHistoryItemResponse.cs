namespace ProjectAtmaca.Api.Decisions.ListApplicationHistory;

public sealed record DecisionApplicationHistoryItemResponse(
    Guid DecisionApplicationId,
    Guid DecisionId,
    string TargetTypeCode,
    Guid TargetId,
    int AppliedDecisionRevision,
    DateTimeOffset AppliedAtUtc);
