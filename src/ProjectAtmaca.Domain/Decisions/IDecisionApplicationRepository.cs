namespace ProjectAtmaca.Domain.Decisions;

public interface IDecisionApplicationRepository
{
    Task AddAsync(
        DecisionApplication decisionApplication,
        CancellationToken cancellationToken = default);
}
