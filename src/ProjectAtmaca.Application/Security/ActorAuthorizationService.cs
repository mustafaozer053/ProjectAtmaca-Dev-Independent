using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Security;

public sealed class ActorAuthorizationService
    : IActorAuthorizationService
{
    private readonly ICurrentActor
        _currentActor;

    private readonly IActorPermissionEvaluator
        _permissionEvaluator;

    public ActorAuthorizationService(
        ICurrentActor currentActor,
        IActorPermissionEvaluator permissionEvaluator)
    {
        ArgumentNullException.ThrowIfNull(
            currentActor);

        ArgumentNullException.ThrowIfNull(
            permissionEvaluator);

        _currentActor =
            currentActor;

        _permissionEvaluator =
            permissionEvaluator;
    }

    public async Task<Result> AuthorizeAsync(
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            permission);

        PermissionDecision decision =
            await _permissionEvaluator.EvaluateAsync(
                _currentActor.ActorId,
                permission,
                cancellationToken);

        if (decision == PermissionDecision.Granted)
        {
            return Result.Success();
        }

        return Result.Failure(
            ActorAuthorizationErrors.Forbidden);
    }
}
