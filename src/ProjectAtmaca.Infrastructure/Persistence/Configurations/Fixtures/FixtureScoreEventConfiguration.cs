using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.Fixtures;

public sealed class FixtureScoreEventConfiguration
    : IEntityTypeConfiguration<FixtureScoreEvent>
{
    public void Configure(EntityTypeBuilder<FixtureScoreEvent> builder)
    {
        builder.ToTable("FixtureScoreEvents");
        builder.HasKey("Id");
        builder.Property<Guid>("Id").ValueGeneratedNever();
        builder.Property(x => x.Side).HasConversion<int>().IsRequired();
        builder.Property(x => x.ScoreTypeCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScoreValue).IsRequired();
        builder.Property(x => x.Minute);
        builder.Property(x => x.AtmacaCardId);
        builder.HasIndex("FixtureId", nameof(FixtureScoreEvent.Side),
            nameof(FixtureScoreEvent.ScoreTypeCode), nameof(FixtureScoreEvent.Minute));
        builder.HasIndex("AtmacaCardId");
    }
}
