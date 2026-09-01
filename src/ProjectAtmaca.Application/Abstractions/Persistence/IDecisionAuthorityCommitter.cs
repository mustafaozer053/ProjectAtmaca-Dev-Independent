using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Application.Abstractions.Persistence;

public interface IDecisionAuthorityCommitter
{
    Task<DecisionAuthorityCommitOutcome> CommitAsync(
        DecisionApplicationOperationId operationId,
        DecisionId decisionId,
        DecisionRevision expectedRevision,
        CancellationToken cancellationToken = default);
}

public enum DecisionAuthorityCommitOutcome
{
    Committed = 1,
    AuthorityLost = 2,
    OperationAlreadyExists = 3
}
