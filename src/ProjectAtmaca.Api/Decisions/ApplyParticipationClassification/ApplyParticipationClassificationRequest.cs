namespace ProjectAtmaca.Api.Decisions.ApplyParticipationClassification;

public sealed record ApplyParticipationClassificationRequest(
    string? OperationId,
    int DecisionRevision,
    string? AppliedAtUtc);
