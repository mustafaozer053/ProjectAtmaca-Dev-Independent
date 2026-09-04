namespace ProjectAtmaca.Domain.Actors;

public readonly record struct ActorId
{
    public Guid Value { get; }

    private ActorId(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor id cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public static ActorId New()
    {
        return new ActorId(
            Guid.NewGuid());
    }

    public static ActorId From(
        Guid value)
    {
        return new ActorId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}