using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Security;

public sealed class ActorClaimsTransformation
    : IClaimsTransformation
{
    private const string IssuerClaimType =
        "iss";

    private const string SubjectClaimType =
        "sub";

    private readonly IActorIdentityResolver _actorIdentityResolver;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActorClaimsTransformation(
        IActorIdentityResolver actorIdentityResolver,
        IHttpContextAccessor httpContextAccessor)
    {
        _actorIdentityResolver =
            actorIdentityResolver
            ?? throw new ArgumentNullException(
                nameof(actorIdentityResolver));

        _httpContextAccessor =
            httpContextAccessor
            ?? throw new ArgumentNullException(
                nameof(httpContextAccessor));
    }

    public async Task<ClaimsPrincipal> TransformAsync(
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(
            principal);

        RemoveUntrustedActorClaims(
            principal);

        bool hasResolutionIdentity =
            principal
                .Identities
                .Any(
                    identity =>
                        string.Equals(
                            identity.AuthenticationType,
                            ActorClaimTypes
                                .ResolutionAuthenticationType,
                            StringComparison.Ordinal));

        if (hasResolutionIdentity)
        {
            return principal;
        }

        bool isAuthenticated =
            principal
                .Identities
                .Any(
                    identity =>
                        identity.IsAuthenticated);

        if (!isAuthenticated)
        {
            return principal;
        }

        Claim[] issuerClaims =
            principal
                .FindAll(
                    IssuerClaimType)
                .ToArray();

        Claim[] subjectClaims =
            principal
                .FindAll(
                    SubjectClaimType)
                .ToArray();

        if (
            issuerClaims.Length != 1 ||
            subjectClaims.Length != 1 ||
            string.IsNullOrWhiteSpace(
                issuerClaims[0].Value) ||
            string.IsNullOrWhiteSpace(
                subjectClaims[0].Value))
        {
            return principal;
        }

        ExternalIdentity externalIdentity =
            ExternalIdentity.Create(
                issuerClaims[0].Value,
                subjectClaims[0].Value);

        CancellationToken cancellationToken =
            _httpContextAccessor
                .HttpContext?
                .RequestAborted
            ?? CancellationToken.None;

        Result<ActorId> resolution =
            await _actorIdentityResolver.ResolveAsync(
                externalIdentity,
                cancellationToken);

        if (
            resolution.IsFailure ||
            resolution.Value.Value == Guid.Empty)
        {
            return principal;
        }

        ClaimsIdentity resolutionIdentity =
            new(
                [
                    new Claim(
                        ActorClaimTypes.ActorId,
                        resolution.Value
                            .Value
                            .ToString("D"))
                ],
                ActorClaimTypes
                    .ResolutionAuthenticationType);

        principal.AddIdentity(
            resolutionIdentity);

        return principal;
    }

    private static void RemoveUntrustedActorClaims(
        ClaimsPrincipal principal)
    {
        ClaimsIdentity[] untrustedIdentities =
            principal
                .Identities
                .Where(
                    identity =>
                        !string.Equals(
                            identity.AuthenticationType,
                            ActorClaimTypes
                                .ResolutionAuthenticationType,
                            StringComparison.Ordinal))
                .ToArray();

        foreach (
            ClaimsIdentity identity
            in untrustedIdentities)
        {
            Claim[] spoofedClaims =
                identity
                    .FindAll(
                        ActorClaimTypes.ActorId)
                    .ToArray();

            foreach (
                Claim spoofedClaim
                in spoofedClaims)
            {
                identity.TryRemoveClaim(
                    spoofedClaim);
            }
        }
    }
}