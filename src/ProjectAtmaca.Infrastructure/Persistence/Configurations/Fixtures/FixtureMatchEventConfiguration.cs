using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.Fixtures;

public sealed class FixtureMatchEventConfiguration
    : IEntityTypeConfiguration<FixtureMatchEvent>
{
    public void Configure(EntityTypeBuilder<FixtureMatchEvent> builder)
    {
        builder.ToTable("FixtureMatchEvents");
        builder.HasKey("Id");
        builder.Property<Guid>("Id").ValueGeneratedNever();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.Minute).IsRequired();
        builder.Property(x => x.AtmacaCardId);
        builder.Property(x => x.RelatedAtmacaCardId);
        builder.HasIndex("FixtureId", nameof(FixtureMatchEvent.Minute));
    }
}
