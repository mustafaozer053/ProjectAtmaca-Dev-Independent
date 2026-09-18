using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.Trainings;

public sealed class TrainingConfiguration : IEntityTypeConfiguration<Training>
{
    public void Configure(EntityTypeBuilder<Training> builder)
    {
        builder.ToTable("Trainings");
        builder.HasKey("Id");
        builder.Property("Id").HasColumnName("Id").ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasConversion(x => x.Value, x => TrainingTitle.Create(x).Value!)
            .HasMaxLength(TrainingTitle.MaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasConversion(x => x.Value, x => TrainingDescription.Create(x).Value!)
            .HasMaxLength(TrainingDescription.MaxLength)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasConversion(x => x.Value, x => TrainingLocation.Create(x).Value!)
            .HasMaxLength(TrainingLocation.MaxLength)
            .IsRequired();

        builder.Property(x => x.Schedule)
            .HasConversion(
                x => $"{x.Date:yyyy-MM-dd}|{x.StartTime:HH\\:mm\\:ss}|{x.EndTime:HH\\:mm\\:ss}",
                x => DeserializeSchedule(x))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SeasonOrganization)
            .HasConversion(
                x => $"{x.SeasonId.Value:D}|{x.OrganizationId.Value:D}",
                x => DeserializeSeasonOrganization(x))
            .HasMaxLength(73)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByActorId)
            .HasConversion(x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? ActorId.From(x.Value) : null)
            .IsRequired(false);
        builder.Property(x => x.LastModifiedAtUtc).IsRequired(false);
        builder.Property(x => x.LastModifiedByActorId)
            .HasConversion(x => x.HasValue ? x.Value.Value : (Guid?)null,
                x => x.HasValue ? ActorId.From(x.Value) : null)
            .IsRequired(false);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Ignore(x => x.TrainingId);
        builder.Ignore(x => x.TrainingTypeAssignments);
        builder.Ignore(x => x.DomainEvents);
    }

    private static TrainingSchedule DeserializeSchedule(string value)
    {
        string[] parts = value.Split('|');
        if (parts.Length != 3 ||
            !DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", out DateOnly date) ||
            !TimeOnly.TryParse(parts[1], out TimeOnly start) ||
            !TimeOnly.TryParse(parts[2], out TimeOnly end))
        {
            throw new InvalidOperationException($"Invalid training schedule '{value}'.");
        }

        return TrainingSchedule.Create(date, start, end).Value!;
    }

    private static SeasonOrganization DeserializeSeasonOrganization(string value)
    {
        string[] parts = value.Split('|');
        if (parts.Length != 2 ||
            !Guid.TryParse(parts[0], out Guid seasonId) ||
            !Guid.TryParse(parts[1], out Guid organizationId))
        {
            throw new InvalidOperationException($"Invalid season organization '{value}'.");
        }

        return SeasonOrganization.Create(
            SeasonId.From(seasonId),
            OrganizationId.From(organizationId));
    }
}
