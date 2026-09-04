namespace ProjectAtmaca.Application.Abstractions.Security;

public sealed record ExternalIdentity
{
    public string Issuer { get; }

    public string Subject { get; }

    private ExternalIdentity(
        string issuer,
        string subject)
    {
        Issuer = issuer;
        Subject = subject;
    }

    public static ExternalIdentity Create(
        string issuer,
        string subject)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ArgumentException(
                "External identity issuer cannot be empty.",
                nameof(issuer));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException(
                "External identity subject cannot be empty.",
                nameof(subject));
        }

        return new ExternalIdentity(
            issuer,
            subject);
    }
}