using System.Diagnostics.Metrics;

namespace ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

public interface IDecisionApplicationMetrics
{
    void RecordAppliedOutcome();

    void RecordReplayOutcome();

    void RecordRejectedOutcome(
        string reason);
}

public sealed class DecisionApplicationMetrics
    : IDecisionApplicationMetrics
{
    public const string MeterName =
        "ProjectAtmaca.Application.DecisionApplication";

    public const string OutcomeCounterName =
        "projectatmaca.decision_application.outcomes";

    private readonly Counter<long> _outcomeCounter;

    public DecisionApplicationMetrics(
        IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(
            meterFactory);

        Meter meter =
            meterFactory.Create(
                new MeterOptions(
                    MeterName));

        _outcomeCounter =
            meter.CreateCounter<long>(
                OutcomeCounterName,
                unit: "{operation}",
                description:
                    "Final decision-application outcomes.");
    }

    public void RecordAppliedOutcome()
    {
        _outcomeCounter.Add(
            1,
            new KeyValuePair<string, object?>(
                "outcome",
                "applied"));
    }

    public void RecordReplayOutcome()
    {
        _outcomeCounter.Add(
            1,
            new KeyValuePair<string, object?>(
                "outcome",
                "replay"));
    }

    public void RecordRejectedOutcome(
        string reason)
    {
        _outcomeCounter.Add(
            1,
            new KeyValuePair<string, object?>(
                "outcome",
                "rejected"),
            new KeyValuePair<string, object?>(
                "reason",
                reason));
    }
}