using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Auditing;

namespace ProjectAtmaca.Api.Tests.Infrastructure;

internal static class
    ProjectAtmacaDatabaseServiceCollectionExtensions
{
    public static IServiceCollection
        ReplaceProjectAtmacaDatabase(
            this IServiceCollection services,
            string connectionString)
    {
        services.RemoveAll<
            ProjectAtmacaDbContext>();

        services.RemoveAll<
            DbContextOptions<
                ProjectAtmacaDbContext>>();

        services.AddScoped<
            DbContextOptions<
                ProjectAtmacaDbContext>>(
            serviceProvider =>
                new DbContextOptionsBuilder<
                    ProjectAtmacaDbContext>()
                    .UseSqlServer(
                        connectionString)
                    .AddInterceptors(
                        serviceProvider
                            .GetRequiredService<
                                AuditableEntitySaveChangesInterceptor>())
                    .Options);

        services.AddScoped<
            ProjectAtmacaDbContext>();

        return services;
    }
}
