using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Fixtures;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.Fixtures;

public sealed class FixtureConfiguration : IEntityTypeConfiguration<Fixture>
{
    public void Configure(EntityTypeBuilder<Fixture> builder)
    {
        builder.ToTable("Fixtures");
        builder.HasKey("Id");
        builder.Property<Guid>("Id").ValueGeneratedNever();
        builder.Property(x => x.SeasonTeamId).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.Opponent).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Date).HasColumnType("date").IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time").IsRequired();
        builder.Property(x => x.Venue).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VenueSide).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(FixtureStatus.Scheduled)
            .HasSentinel(FixtureStatus.Scheduled)
            .IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.DurationMinutes);
        builder.Property(x => x.Referee).HasMaxLength(200);
        builder.Property(x => x.MatchNotes).HasMaxLength(2000);
        builder.HasOne<SeasonTeam>()
            .WithMany()
            .HasForeignKey(x => x.SeasonTeamId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.SquadMembers)
            .WithOne()
            .HasForeignKey("FixtureId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.SquadMembers)
            .HasField("_squadMembers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.MatchEvents)
            .WithOne()
            .HasForeignKey("FixtureId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.MatchEvents)
            .HasField("_matchEvents")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.ScoreEvents)
            .WithOne()
            .HasForeignKey("FixtureId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.ScoreEvents)
            .HasField("_scoreEvents")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Corrections)
            .WithOne()
            .HasForeignKey("FixtureId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Corrections)
            .HasField("_corrections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByActorId)
            .HasConversion(x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? ActorId.From(x.Value) : null);
        builder.Property(x => x.LastModifiedAtUtc);
        builder.Property(x => x.LastModifiedByActorId)
            .HasConversion(x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? ActorId.From(x.Value) : null);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(x => x.DomainEvents);
        builder.HasIndex("SeasonTeamId", "Date", "StartTime");
    }
}
