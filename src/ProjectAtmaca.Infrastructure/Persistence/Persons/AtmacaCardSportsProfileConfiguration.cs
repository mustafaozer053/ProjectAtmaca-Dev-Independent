using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class AtmacaCardSportsProfileConfiguration
    : IEntityTypeConfiguration<AtmacaCardSportsProfile>
{
    public void Configure(EntityTypeBuilder<AtmacaCardSportsProfile> builder)
    {
        builder.ToTable("AtmacaCardSportsProfiles");
        RegistrationMapping.Audit(builder);
        builder.Property(x => x.AtmacaCardId).IsRequired();
        builder.Property(x => x.SportName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.LicenseNumber)
            .HasConversion(
                value => value == null ? null : value.Value,
                value => value == null ? null : LicenseNumber.Create(value).Value)
            .HasMaxLength(100);
        builder.Property(x => x.StartedSportOn).HasColumnType("date");
        builder.Property(x => x.ClubRegisteredOn).HasColumnType("date");
        builder.Property(x => x.CompetitionLevel).HasConversion<int?>();
        builder.HasIndex(x => new { x.AtmacaCardId, x.SportName }).IsUnique();
        builder.HasOne<AtmacaCard>()
            .WithMany(x => x.SportsProfiles)
            .HasForeignKey(x => x.AtmacaCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Positions)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "AtmacaCardSportsProfilePositions",
                right => right.HasOne<Position>()
                    .WithMany()
                    .HasForeignKey("PositionId")
                    .OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<AtmacaCardSportsProfile>()
                    .WithMany()
                    .HasForeignKey("AtmacaCardSportsProfileId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("AtmacaCardSportsProfilePositions");
                    join.HasKey("AtmacaCardSportsProfileId", "PositionId");
                    join.HasIndex("PositionId");
                });
        builder.Navigation(x => x.Positions)
            .HasField("_positions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
