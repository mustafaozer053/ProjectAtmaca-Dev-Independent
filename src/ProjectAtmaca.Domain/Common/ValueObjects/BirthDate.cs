namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class BirthDate : ValueObject
{
    public DateTime Value { get; }

    private BirthDate(DateTime value)
    {
        Value = value.Date;
    }

    public static BirthDate Create(DateTime value)
    {
        var today = DateTime.UtcNow.Date;

        if (value.Date > today)
            throw new ArgumentException("Birth date cannot be in the future.");

        return new BirthDate(value);
    }

    public int GetAge()
    {
        var today = DateTime.UtcNow.Date;

        var age = today.Year - Value.Year;

        if (Value.Date > today.AddYears(-age))
            age--;

        return age;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value.ToString("yyyy-MM-dd");
    }
}
