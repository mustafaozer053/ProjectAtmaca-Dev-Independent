using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class AtmacaCardMeasurementConfiguration
    : IEntityTypeConfiguration<AtmacaCardMeasurement>
{
    public void Configure(EntityTypeBuilder<AtmacaCardMeasurement> builder)
    {
        builder.ToTable("AtmacaCardMeasurements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AtmacaCardId).IsRequired();
        builder.Property(x => x.MeasuredOn).HasColumnType("date").IsRequired();
        builder.Property(x => x.HeightCentimeters).HasPrecision(5, 2);
        builder.Property(x => x.WeightKilograms).HasPrecision(5, 2);
        builder.HasIndex(x => new { x.AtmacaCardId, x.MeasuredOn }).IsUnique();
        builder.HasOne<AtmacaCard>()
            .WithMany(x => x.Measurements)
            .HasForeignKey(x => x.AtmacaCardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
