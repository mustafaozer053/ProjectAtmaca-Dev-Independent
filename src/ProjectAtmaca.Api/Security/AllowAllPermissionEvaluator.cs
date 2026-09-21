using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Api.Security;

/// <summary>
/// Development-only permission evaluator that grants every permission
/// to every actor. This removes the need to seed
/// <c>ActorPermissionGrant</c> rows while prototyping locally. It must
/// never be registered outside <c>Development</c>.
/// </summary>
public sealed class AllowAllPermissionEvaluator
    : IActorPermissionEvaluator
{
    public Task<PermissionDecision> EvaluateAsync(
        ActorId actorId,
        Permission permission,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PermissionDecision.Granted);
}
