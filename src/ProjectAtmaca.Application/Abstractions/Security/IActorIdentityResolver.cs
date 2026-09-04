using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Abstractions.Security;

public interface IActorIdentityResolver
{
    Task<Result<ActorId>> ResolveAsync(
        ExternalIdentity externalIdentity,
        CancellationToken cancellationToken = default);
}