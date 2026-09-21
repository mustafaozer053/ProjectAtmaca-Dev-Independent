using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProjectAtmaca.Api.Security;

/// <summary>
/// Development-only authentication scheme that authenticates every
/// request as a fixed local actor. This must never be registered
/// or activated outside <c>Development</c>. It exists purely to let
/// a local UI client (e.g. the Blazor prototype) call the API without
/// standing up a full OpenID Connect identity provider.
/// </summary>
public sealed class DevelopmentActorAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevelopmentActor";

    public static readonly Guid DevActorId =
        Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    public DevelopmentActorAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ClaimsIdentity resolutionIdentity =
            new(
                [
                    new Claim(
                        ActorClaimTypes.ActorId,
                        DevActorId.ToString("D"))
                ],
                ActorClaimTypes.ResolutionAuthenticationType);

        ClaimsPrincipal principal = new(resolutionIdentity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
