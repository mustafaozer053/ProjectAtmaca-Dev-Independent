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

            bool resolved =
                ResolvedActorPrincipal.TryGetActorId(
                    principal,
                    out ActorId actorId);

            if (!resolved)
            {
                throw new InvalidOperationException(
                    MissingOrInvalidActorMessage);
            }

            return actorId;
        }
    }
}