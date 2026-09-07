using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Infrastructure.Persistence.Security;

public sealed class ActorPermissionGrant
{
    public long Id { get; private set; }

    public ActorId ActorId { get; private set; }

    public string PermissionCode { get; private set; } =
        null!;

    private ActorPermissionGrant()
    {
    }

    private ActorPermissionGrant(
        ActorId actorId,
        Permission permission)
    {
        ActorId =
            actorId;

        PermissionCode =
            permission.Code;
    }

    public static ActorPermissionGrant Create(
        ActorId actorId,
        Permission permission)
    {
        if (actorId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor id cannot be empty.",
                nameof(actorId));
        }

        ArgumentNullException.ThrowIfNull(
            permission);

        return new ActorPermissionGrant(
            actorId,
            permission);
    }
}