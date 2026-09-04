using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Infrastructure.Persistence.Security;

public sealed class ActorIdentityMapping
{
    public long Id { get; private set; }

    public string Issuer { get; private set; } =
        null!;

    public string Subject { get; private set; } =
        null!;

    public ActorId ActorId { get; private set; }

    private ActorIdentityMapping()
    {
    }

    private ActorIdentityMapping(
        ExternalIdentity externalIdentity,
        ActorId actorId)
    {
        Issuer = externalIdentity.Issuer;
        Subject = externalIdentity.Subject;
        ActorId = actorId;
    }

    public static ActorIdentityMapping Create(
        ExternalIdentity externalIdentity,
        ActorId actorId)
    {
        ArgumentNullException.ThrowIfNull(
            externalIdentity);

        if (actorId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor id cannot be empty.",
                nameof(actorId));
        }

        return new ActorIdentityMapping(
            externalIdentity,
            actorId);
    }
}