using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Api.Security;

public sealed class HttpContextCurrentActor
    : ICurrentActor
{
    private const string MissingOrInvalidActorMessage =
        "A single valid resolved actor identity is required " +
        "for the current request.";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentActor(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor
            ?? throw new ArgumentNullException(
                nameof(httpContextAccessor));
    }

    public ActorId ActorId
    {
        get
        {
            ClaimsPrincipal? principal =
                _httpContextAccessor
                    .HttpContext?
                    .User;

            Claim[] actorClaims =
                principal?
                    .Identities
                    .Where(
                        identity =>
                            string.Equals(
                                identity.AuthenticationType,
                                ActorClaimTypes
                                    .ResolutionAuthenticationType,
                                StringComparison.Ordinal))
                    .SelectMany(
                        identity =>
                            identity.FindAll(
                                ActorClaimTypes.ActorId))
                    .ToArray()
                ?? [];

            if (actorClaims.Length != 1)
            {
                throw new InvalidOperationException(
                    MissingOrInvalidActorMessage);
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
                throw new InvalidOperationException(
                    MissingOrInvalidActorMessage);
            }

            return global::ProjectAtmaca.Domain.Actors
                .ActorId.From(
                    actorIdValue);
        }
    }
}