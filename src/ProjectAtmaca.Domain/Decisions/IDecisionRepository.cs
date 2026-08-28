namespace ProjectAtmaca.Domain.Decisions;

public interface IDecisionRepository
{
    Task<Decision?> GetByIdAsync(
        DecisionId id,
        CancellationToken cancellationToken = default);
}
