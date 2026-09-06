using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Infrastructure.Persistence.Auditing;

public sealed class AuditableEntitySaveChangesInterceptor
    : SaveChangesInterceptor
{
    private readonly ICurrentActor _currentActor;

    public AuditableEntitySaveChangesInterceptor(
        ICurrentActor currentActor)
    {
        ArgumentNullException.ThrowIfNull(
            currentActor);

        _currentActor =
            currentActor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyCanonicalAuditIdentity(
            eventData.Context);

        return base.SavingChanges(
            eventData,
            result);
    }

    public override ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        ApplyCanonicalAuditIdentity(
            eventData.Context);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private void ApplyCanonicalAuditIdentity(
        DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        EntityEntry<AuditableAggregateRoot>[] auditableEntries =
            context.ChangeTracker
                .Entries<AuditableAggregateRoot>()
                .Where(
                    entry =>
                        entry.State == EntityState.Added
                        || entry.State == EntityState.Modified)
                .ToArray();

        if (auditableEntries.Length == 0)
        {
            return;
        }

        ActorId actorId =
            _currentActor.ActorId;

        foreach (
            EntityEntry<AuditableAggregateRoot> entry
            in auditableEntries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedByActorId is null)
                {
                    entry.Entity.SetCreatedBy(
                        actorId);
                }

                continue;
            }

            entry.Entity.MarkAsModified(
                actorId);
        }
    }
}
