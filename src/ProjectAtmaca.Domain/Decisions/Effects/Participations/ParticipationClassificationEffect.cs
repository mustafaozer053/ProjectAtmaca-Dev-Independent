namespace ProjectAtmaca.Domain.Decisions.Effects.Participations;

public readonly record struct ParticipationClassificationEffect
{
    public ParticipationClassificationOutcome Outcome { get; }

    private ParticipationClassificationEffect(
        ParticipationClassificationOutcome outcome)
    {
        Outcome = outcome;
    }

    public static ParticipationClassificationEffect Present()
    {
        return new ParticipationClassificationEffect(
            ParticipationClassificationOutcome.Present);
    }

    public static ParticipationClassificationEffect Absent()
    {
        return new ParticipationClassificationEffect(
            ParticipationClassificationOutcome.Absent);
    }
}
