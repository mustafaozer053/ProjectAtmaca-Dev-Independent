using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Application.Abstractions.Security;

public interface IActorPermissionEvaluator
{
    Task<PermissionDecision> EvaluateAsync(
        ActorId actorId,
        Permission permission,
        CancellationToken cancellationToken = default);
}