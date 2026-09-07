using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Abstractions.Security;

public static class ActorAuthorizationErrors
{
    public static readonly Error Forbidden =
        Error.Create(
            "Security.Authorization.Forbidden",
            "The current actor is not authorized " +
            "to perform this operation.");
}
