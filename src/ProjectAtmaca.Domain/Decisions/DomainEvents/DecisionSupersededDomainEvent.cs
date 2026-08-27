using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Decisions.DomainEvents;

public sealed class DecisionSupersededDomainEvent
    : DomainEvent
{
    public DecisionId DecisionId { get; }

    public DecisionId SuccessorDecisionId { get; }

    public DecisionSupersededDomainEvent(
        DecisionId decisionId,
        DecisionId successorDecisionId)
    {
        DecisionId = decisionId;
        SuccessorDecisionId =
            successorDecisionId;
    }
}
