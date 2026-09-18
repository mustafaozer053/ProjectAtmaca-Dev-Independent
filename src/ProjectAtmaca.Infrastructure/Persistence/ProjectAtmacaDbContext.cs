using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;

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

    public DbSet<Training> Trainings =>
        Set<Training>();

    public DbSet<TrainingType> TrainingTypes =>
        Set<TrainingType>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasSequence<long>("AtmacaCardNumbers", "dbo")
            .StartsAt(1).IncrementsBy(1).HasMin(1).HasMax(999999).IsCyclic(false);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ProjectAtmacaDbContext).Assembly);
    }
}
