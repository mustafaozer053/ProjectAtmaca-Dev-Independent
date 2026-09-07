using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Security;

public sealed class ActorPermissionEvaluator
    : IActorPermissionEvaluator
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public ActorPermissionEvaluator(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<PermissionDecision> EvaluateAsync(
        ActorId actorId,
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            permission);

        if (actorId.Value == Guid.Empty)
        {
            return PermissionDecision.Denied;
        }

        int permissionCodeByteLength =
            checked(
                permission.Code.Length *
                sizeof(char));

        bool exactGrantExists =
            await _dbContext
                .Set<ActorPermissionGrant>()
                .AsNoTracking()
                .AnyAsync(
                    grant =>
                        grant.ActorId == actorId &&
                        grant.PermissionCode ==
                            permission.Code &&
                        EF.Property<int>(
                            grant,
                            "PermissionCodeByteLength") ==
                            permissionCodeByteLength,
                    cancellationToken);

        return exactGrantExists
            ? PermissionDecision.Granted
            : PermissionDecision.Denied;
    }
}