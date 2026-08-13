using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Infrastructure.Persistence;

public sealed class ProjectAtmacaDbContext
    : DbContext
{
    public ProjectAtmacaDbContext(
        DbContextOptions<ProjectAtmacaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Participation> Participations =>
        Set<Participation>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ProjectAtmacaDbContext).Assembly);
    }
}
