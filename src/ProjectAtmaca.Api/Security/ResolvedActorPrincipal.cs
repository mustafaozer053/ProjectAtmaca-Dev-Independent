using System.Security.Claims;

using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Api.Security;

internal static class ResolvedActorPrincipal
{
    public static bool TryGetActorId(
        ClaimsPrincipal? principal,
        out ActorId actorId)
    {
        actorId =
            default;

        if (principal is null)
        {
            return false;
        }

        ClaimsIdentity[] resolutionIdentities =
            principal
                .Identities
                .Where(
                    identity =>
                        string.Equals(
                            identity.AuthenticationType,
                            ActorClaimTypes
                                .ResolutionAuthenticationType,
                            StringComparison.Ordinal))
                .ToArray();

        if (resolutionIdentities.Length != 1)
        {
            return false;
        }

        Claim[] actorClaims =
            resolutionIdentities[0]
                .FindAll(
                    ActorClaimTypes.ActorId)
                .ToArray();

        if (actorClaims.Length != 1)
        {
            return false;
        }

        bool parsed =
            Guid.TryParseExact(
                actorClaims[0].Value,
                "D",
                out Guid actorIdValue);

        if (
            !parsed ||
            actorIdValue == Guid.Empty)
        {
            return false;
        }

        actorId =
            ActorId.From(
                actorIdValue);

        return true;
    }
}