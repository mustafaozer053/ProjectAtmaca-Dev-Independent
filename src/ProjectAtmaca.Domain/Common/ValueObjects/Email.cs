using System.Text.RegularExpressions;

namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.");

        value = value.Trim().ToLowerInvariant();

        if (!IsValid(value))
            throw new ArgumentException("Email format is invalid.");

        return new Email(value);
    }

    private static bool IsValid(string email)
    {
        return Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
