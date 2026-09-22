using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.AgeGroups;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations;

public sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Seasons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .HasConversion(x => x.Value, x => SeasonName.Create(x).Value!)
            .HasMaxLength(9)
            .IsRequired();
        builder.Property(x => x.Period)
            .HasConversion(
                x => $"{x.StartDate:yyyy-MM-dd}|{x.EndDate:yyyy-MM-dd}",
                x => DeserializeDateRange(x))
            .HasMaxLength(21)
            .IsRequired();
        ConfigureAudit(builder);
    }

    private static DateRange DeserializeDateRange(string value)
    {
        var parts = value.Split('|');
        return DateRange.Create(
            DateTime.Parse(parts[0]),
            DateTime.Parse(parts[1]));
    }

    private static void ConfigureAudit(EntityTypeBuilder<Season> builder)
    {
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByActorId).HasConversion(
            x => x.HasValue ? x.Value.Value : (Guid?)null,
            x => x.HasValue ? ProjectAtmaca.Domain.Actors.ActorId.From(x.Value) : null);
        builder.Property(x => x.LastModifiedAtUtc).IsRequired(false);
        builder.Property(x => x.LastModifiedByActorId).HasConversion(
            x => x.HasValue ? x.Value.Value : (Guid?)null,
            x => x.HasValue ? ProjectAtmaca.Domain.Actors.ActorId.From(x.Value) : null);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Ignore(x => x.OrganizationId);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByActorId).HasConversion(
            x => x.HasValue ? x.Value.Value : (Guid?)null,
            x => x.HasValue ? ProjectAtmaca.Domain.Actors.ActorId.From(x.Value) : null);
        builder.Property(x => x.LastModifiedAtUtc).IsRequired(false);
        builder.Property(x => x.LastModifiedByActorId).HasConversion(
            x => x.HasValue ? x.Value.Value : (Guid?)null,
            x => x.HasValue ? ProjectAtmaca.Domain.Actors.ActorId.From(x.Value) : null);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

public sealed class AgeGroupConfiguration : IEntityTypeConfiguration<AgeGroup>
{
    public void Configure(EntityTypeBuilder<AgeGroup> builder)
    {
        builder.ToTable("AgeGroups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code)
            .HasConversion(x => x.Value, x => AgeGroupCode.Create(x).Value!)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByActorId).HasConversion(
            x => x.HasValue ? x.Value.Value : (Guid?)null,
            x => x.HasValue ? ProjectAtmaca.Domain.Actors.ActorId.From(x.Value) : null);
        builder.Property(x => x.LastModifiedAtUtc).IsRequired(false);
        builder.Property(x => x.LastModifiedByActorId).HasConversion(
            x => x.HasValue ? x.Value.Value : (Guid?)null,
            x => x.HasValue ? ProjectAtmaca.Domain.Actors.ActorId.From(x.Value) : null);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(x => x.DomainEvents);
    }
}
