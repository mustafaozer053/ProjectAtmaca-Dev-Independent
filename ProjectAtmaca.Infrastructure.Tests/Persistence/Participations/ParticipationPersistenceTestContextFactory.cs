using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Infrastructure.Persistence;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

internal static class ParticipationPersistenceTestContextFactory
{
    internal const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;" +
        "Database=ProjectAtmaca_IntegrationTests;" +
        "Trusted_Connection=True;" +
        "TrustServerCertificate=True";

    public static ProjectAtmacaDbContext CreateContext()
    {
        DbContextOptions<ProjectAtmacaDbContext> options =
            new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

        return new ProjectAtmacaDbContext(options);
    }
}
