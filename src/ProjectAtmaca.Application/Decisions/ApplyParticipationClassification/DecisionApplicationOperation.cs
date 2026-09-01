using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

public sealed record DecisionApplicationOperation(
    DecisionApplicationOperationId OperationId,
    DecisionId DecisionId,
    DecisionRevision DecisionRevision,
    DateTimeOffset AppliedAtUtc);
