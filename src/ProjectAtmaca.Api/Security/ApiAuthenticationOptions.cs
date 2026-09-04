namespace ProjectAtmaca.Api.Security;

public sealed class ApiAuthenticationOptions
{
    public const string SectionName =
        "Authentication";

    public string Authority { get; set; } =
        string.Empty;

    public string Audience { get; set; } =
        string.Empty;
}