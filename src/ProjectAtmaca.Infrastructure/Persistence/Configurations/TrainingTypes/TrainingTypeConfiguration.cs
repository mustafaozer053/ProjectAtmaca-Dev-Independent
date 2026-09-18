using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.TrainingTypes;

public sealed class TrainingTypeConfiguration
    : IEntityTypeConfiguration<TrainingType>
{
    public void Configure(EntityTypeBuilder<TrainingType> builder)
    {
        builder.ToTable("TrainingTypes");
        builder.HasKey("Id");
        builder.Property("Id")
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasConversion(x => x.Value, x => TrainingTypeCode.Create(x).Value!)
            .HasMaxLength(TrainingTypeCode.MaxLength)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasConversion(x => x.Value, x => TrainingTypeName.Create(x).Value!)
            .HasMaxLength(TrainingTypeName.MaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasConversion(
                x => x.Value,
                x => TrainingTypeDescription.Create(x).Value!)
            .HasMaxLength(TrainingTypeDescription.MaxLength)
            .IsRequired();

        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByActorId)
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? ActorId.From(x.Value) : null)
            .IsRequired(false);
        builder.Property(x => x.LastModifiedAtUtc).IsRequired(false);
        builder.Property(x => x.LastModifiedByActorId)
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? ActorId.From(x.Value) : null)
            .IsRequired(false);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.Ignore(x => x.TrainingTypeId);
        builder.Ignore(x => x.DomainEvents);
    }
}
