namespace ProjectAtmaca.Application.Abstractions.Security;

public sealed record Permission
{
    public string Code { get; }

    private Permission(
        string code)
    {
        Code =
            code;
    }

    public static Permission Create(
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Permission code cannot be empty.",
                nameof(code));
        }

        if (
            !string.Equals(
                code,
                code.Trim(),
                StringComparison.Ordinal)
        )
        {
            throw new ArgumentException(
                "Permission code cannot contain leading or trailing whitespace.",
                nameof(code));
        }

        return new Permission(
            code);
    }

    public override string ToString()
    {
        return Code;
    }
}