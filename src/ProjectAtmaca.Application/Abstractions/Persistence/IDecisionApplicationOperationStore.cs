using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

namespace ProjectAtmaca.Application.Abstractions.Persistence;

public interface IDecisionApplicationOperationStore
{
    Task<DecisionApplicationOperation?> GetByIdAsync(
        DecisionApplicationOperationId operationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DecisionApplicationOperation operation,
        CancellationToken cancellationToken = default);
}
