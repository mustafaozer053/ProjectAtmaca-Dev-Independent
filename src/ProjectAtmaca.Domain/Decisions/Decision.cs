using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Decisions.DomainEvents;

namespace ProjectAtmaca.Domain.Decisions;

public sealed class Decision : AggregateRoot
{
    public DecisionId DecisionId =>
        DecisionId.From(Id);

    public DecisionRevision Revision { get; private set; }

    public DecisionTargetReference Target { get; private set; }

    public ParticipationClassificationSnapshot Snapshot { get; private set; }

    public ParticipationClassificationEffect Effect { get; private set; }

    public DecisionId? SupersededByDecisionId { get; private set; }

    private Decision()
    {
    }

    private Decision(
        DecisionId id,
        DecisionTargetReference target,
        ParticipationClassificationSnapshot snapshot,
        ParticipationClassificationEffect effect)
        : base(id.Value)
    {
        Revision =
            DecisionRevision.Initial;

        Target =
            target;

        Snapshot =
            snapshot;

        Effect =
            effect;
    }

    public static Decision CreateParticipationClassification(
        ParticipationId participationId,
        ParticipationClassificationSnapshot snapshot,
        ParticipationClassificationEffect effect)
    {
        return new Decision(
            DecisionId.New(),
            DecisionTargetReference.ForParticipation(
                participationId),
            snapshot,
            effect);
    }

    public void ReEvaluate(
        ParticipationClassificationSnapshot snapshot,
        ParticipationClassificationEffect effect)
    {
        if (SupersededByDecisionId is not null)
        {
            throw new InvalidOperationException(
                "A superseded decision cannot be re-evaluated.");
        }

        if (Snapshot == snapshot &&
            Effect == effect)
        {
            return;
        }

        Revision =
            Revision.Next();

        Snapshot =
            snapshot;

        Effect =
            effect;
    }

    public void SupersedeBy(
        DecisionId successorDecisionId)
    {
        if (successorDecisionId == DecisionId)
        {
            throw new ArgumentException(
                "A decision cannot supersede itself.",
                nameof(successorDecisionId));
        }

        if (SupersededByDecisionId is null)
        {
            SupersededByDecisionId =
                successorDecisionId;

            RaiseDomainEvent(
                new DecisionSupersededDomainEvent(
                    DecisionId,
                    successorDecisionId));

            return;
        }

        if (SupersededByDecisionId.Value ==
            successorDecisionId)
        {
            return;
        }

        throw new InvalidOperationException(
            "A superseded decision cannot be superseded by another decision.");
    }
}
