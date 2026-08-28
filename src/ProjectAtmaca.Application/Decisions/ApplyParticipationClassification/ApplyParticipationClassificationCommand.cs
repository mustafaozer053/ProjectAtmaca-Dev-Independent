using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

public sealed record ApplyParticipationClassificationCommand(
    DecisionId DecisionId,
    DecisionRevision DecisionRevision,
    DateTimeOffset AppliedAtUtc);
