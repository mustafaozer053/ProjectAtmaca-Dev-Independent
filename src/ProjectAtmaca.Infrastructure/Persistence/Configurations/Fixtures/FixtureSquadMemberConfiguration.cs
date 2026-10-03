using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Fixtures;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.Fixtures;

public sealed class FixtureSquadMemberConfiguration
    : IEntityTypeConfiguration<FixtureSquadMember>
{
    public void Configure(EntityTypeBuilder<FixtureSquadMember> builder)
    {
        builder.ToTable("FixtureSquadMembers");
        builder.HasKey("Id");
        builder.Property<Guid>("Id").ValueGeneratedNever();
        builder.Property(x => x.AtmacaCardId).IsRequired();
        builder.Property(x => x.Role).HasConversion<int>().IsRequired();
        builder.HasIndex("FixtureId", nameof(FixtureSquadMember.AtmacaCardId))
            .IsUnique();
    }
}
