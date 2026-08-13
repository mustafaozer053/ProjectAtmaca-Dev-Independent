namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class Country : ValueObject
{
    public string Code { get; }

    public string Name { get; }

    private Country(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public static Country Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Country code cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Country name cannot be empty.");

        code = code.Trim().ToUpperInvariant();
        name = name.Trim();

        if (code.Length != 2)
            throw new ArgumentException("Country code must be ISO Alpha-2 format.");

        if (!code.All(char.IsLetter))
            throw new ArgumentException("Country code must contain only letters.");

        return new Country(code, name);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString()
    {
        return $"{Name} ({Code})";
    }
}
