using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.SeasonTeams;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.AgeGroups;
using ProjectAtmaca.Domain.Fixtures;
using ProjectAtmaca.Domain.Positions;
using ProjectAtmaca.Domain.Scouting;

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

    public DbSet<SeasonTeam> SeasonTeams =>
        Set<SeasonTeam>();

    public DbSet<Season> Seasons => Set<Season>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<AgeGroup> AgeGroups => Set<AgeGroup>();
    public DbSet<Fixture> Fixtures => Set<Fixture>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<ScoutingCandidate> ScoutingCandidates => Set<ScoutingCandidate>();

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
