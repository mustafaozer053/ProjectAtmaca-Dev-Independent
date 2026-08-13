namespace ProjectAtmaca.Domain.Common;

public sealed class Error
{
    public string Code { get; }

    public string Message { get; }

    private Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Error Create(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Error code cannot be empty.");

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be empty.");

        return new Error(code.Trim(), message.Trim());
    }

    public override string ToString()
    {
        return $"{Code}: {Message}";
    }
}
