global using ProjectAtmaca.Infrastructure.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Infrastructure.Tests.Support;

internal static class
    TestCurrentActorServiceCollectionExtensions
{
    private static readonly ActorId CanonicalTestActorId =
        ActorId.From(
            Guid.Parse(
                "7a570000-0000-4000-8000-000000000001"));

    public static IServiceCollection AddTestCurrentActor(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        services.AddScoped<ICurrentActor>(
            _ =>
                new TestCurrentActor(
                    CanonicalTestActorId));

        return services;
    }

    public static async Task<ActorId>
        GrantPermissionToTestCurrentActorAsync(
            this IServiceProvider serviceProvider,
            Permission permission,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            serviceProvider);

        ArgumentNullException.ThrowIfNull(
            permission);

        ICurrentActor currentActor =
            serviceProvider
                .GetRequiredService<ICurrentActor>();

        ProjectAtmacaDbContext dbContext =
            serviceProvider
                .GetRequiredService<
                    ProjectAtmacaDbContext>();

        int permissionCodeByteLength =
            checked(
                permission.Code.Length *
                sizeof(char));

        bool exactGrantExists =
            await dbContext
                .Set<ActorPermissionGrant>()
                .AsNoTracking()
                .AnyAsync(
                    grant =>
                        grant.ActorId ==
                            currentActor.ActorId &&
                        grant.PermissionCode ==
                            permission.Code &&
                        EF.Property<int>(
                            grant,
                            "PermissionCodeByteLength") ==
                            permissionCodeByteLength,
                    cancellationToken);

        if (!exactGrantExists)
        {
            dbContext
                .Set<ActorPermissionGrant>()
                .Add(
                    ActorPermissionGrant.Create(
                        currentActor.ActorId,
                        permission));

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return currentActor.ActorId;
    }

    private sealed class TestCurrentActor :
        ICurrentActor
    {
        public TestCurrentActor(
            ActorId actorId)
        {
            ActorId = actorId;
        }

        public ActorId ActorId { get; }
    }
}