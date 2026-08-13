namespace ProjectAtmaca.Domain.Participations;

public readonly record struct ParticipationId
{
    public Guid Value { get; }

    private ParticipationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Participation id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static ParticipationId New()
    {
        return new ParticipationId(Guid.NewGuid());
    }

    public static ParticipationId From(Guid value)
    {
        return new ParticipationId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
