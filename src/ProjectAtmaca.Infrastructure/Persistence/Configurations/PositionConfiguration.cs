using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Positions;
using ProjectAtmaca.Infrastructure.Persistence.Persons;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        RegistrationMapping.Audit(builder);
        builder.Property(x => x.SportName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.SportName, x.Code }).IsUnique();
    }
}
