using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Abstractions.Security;

public static class ActorIdentityResolutionErrors
{
    public static readonly Error NotMapped =
        Error.Create(
            "Security.ActorIdentity.NotMapped",
            "The external identity is not mapped to an actor.");
}