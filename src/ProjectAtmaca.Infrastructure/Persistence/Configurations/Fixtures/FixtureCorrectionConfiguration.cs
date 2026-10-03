using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.Fixtures;

public sealed class FixtureCorrectionConfiguration
    : IEntityTypeConfiguration<FixtureCorrection>
{
    public void Configure(EntityTypeBuilder<FixtureCorrection> builder)
    {
        builder.ToTable("FixtureCorrections");
        builder.HasKey("Id");
        builder.Property<Guid>("Id").ValueGeneratedNever();
        builder.Property(x => x.Reason).HasMaxLength(
            FixtureCorrection.MaxReasonLength).IsRequired();
        builder.Property(x => x.ReopenedAtUtc).IsRequired();
        builder.Property(x => x.ReopenedByActorId)
            .HasConversion(x => x.Value, x => ActorId.From(x))
            .IsRequired();
        builder.HasIndex("FixtureId", nameof(FixtureCorrection.ReopenedAtUtc));
    }
}
