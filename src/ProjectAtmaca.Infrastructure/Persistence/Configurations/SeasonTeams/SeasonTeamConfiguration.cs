using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Globalization;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.SeasonTeams;

public sealed class SeasonTeamConfiguration : IEntityTypeConfiguration<SeasonTeam>
{
    public void Configure(EntityTypeBuilder<SeasonTeam> builder)
    {
        builder.ToTable("SeasonTeams");
        builder.HasKey("Id");
        builder.Property("Id").HasColumnName("Id").ValueGeneratedNever();

        builder.Property(x => x.SeasonId)
            .HasConversion(id => id.Value, value => SeasonId.From(value))
            .HasColumnName("SeasonId")
            .IsRequired();

        builder.Property(x => x.OrganizationId)
            .HasConversion(id => id.Value, value => OrganizationId.From(value))
            .HasColumnName("OrganizationId")
            .IsRequired();

        builder.Property(x => x.AgeGroupId)
            .HasColumnName("AgeGroupId")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        ConfigureAudit(builder);

        builder.OwnsMany(
            x => x.Memberships,
            memberships =>
            {
                memberships.ToTable("SeasonTeamMemberships");
                memberships.HasKey("Id");
                memberships.Property<Guid>("Id")
                    .HasColumnName("Id")
                    .ValueGeneratedNever();
                memberships.Property<Guid>("SeasonTeamId")
                    .HasColumnName("SeasonTeamId")
                    .IsRequired();
                memberships.Property(x => x.AtmacaCardId)
                    .HasConversion(id => id.Value, value => AtmacaCardId.From(value))
                    .HasColumnName("AtmacaCardId")
                    .IsRequired();
                memberships.Property(x => x.Period)
                    .HasConversion(
                        period => SerializePeriod(period),
                        value => DeserializePeriod(value))
                    .HasColumnName("Period")
                    .HasMaxLength(64)
                    .IsRequired();

                memberships.OwnsMany(
                    x => x.Assignments,
                    assignments =>
                    {
                        assignments.ToTable("SeasonTeamMembershipAssignments");
                        assignments.HasKey("Id");
                        assignments.Property<Guid>("Id")
                            .HasColumnName("Id")
                            .ValueGeneratedNever();
                        assignments.Property<Guid>("SeasonTeamMembershipId")
                            .HasColumnName("SeasonTeamMembershipId")
                            .IsRequired();
                        assignments.Property(x => x.Kind)
                            .HasConversion<int>()
                            .HasColumnName("Kind")
                            .IsRequired();
                        assignments.Property(x => x.DefinitionId)
                            .HasColumnName("DefinitionId")
                            .IsRequired();
                        assignments.Property(x => x.DisplayNameSnapshot)
                            .HasColumnName("DisplayNameSnapshot")
                            .HasMaxLength(200)
                            .IsRequired();
                        assignments.Property(x => x.Period)
                            .HasConversion(
                                period => SerializePeriod(period),
                                value => DeserializePeriod(value))
                            .HasColumnName("Period")
                            .HasMaxLength(64)
                            .IsRequired();
                        assignments.HasIndex(
                            "SeasonTeamMembershipId",
                            nameof(SeasonTeamMembershipAssignment.Kind),
                            nameof(SeasonTeamMembershipAssignment.DefinitionId));
                        assignments.Ignore(x => x.SeasonTeamMembershipAssignmentId);
                    });

                memberships.Ignore(x => x.SeasonTeamMembershipId);
            });

        builder.Ignore(x => x.SeasonTeamId);
        builder.Ignore(x => x.DomainEvents);
    }

    private static void ConfigureAudit(EntityTypeBuilder<SeasonTeam> builder)
    {
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByActorId)
            .HasConversion(
                value => value.HasValue ? value.Value.Value : (Guid?)null,
                value => value.HasValue ? ActorId.From(value.Value) : null)
            .IsRequired(false);
        builder.Property(x => x.LastModifiedAtUtc).IsRequired(false);
        builder.Property(x => x.LastModifiedByActorId)
            .HasConversion(
                value => value.HasValue ? value.Value.Value : (Guid?)null,
                value => value.HasValue ? ActorId.From(value.Value) : null)
            .IsRequired(false);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }

    private static string SerializePeriod(AssignmentPeriod period)
    {
        var endDate = period.EndDate.HasValue
            ? period.EndDate.Value.ToString("yyyy-MM-dd")
            : "open";

        return $"{period.StartDate:yyyy-MM-dd}|{endDate}";
    }

    private static AssignmentPeriod DeserializePeriod(string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 2 ||
            !DateTime.TryParseExact(
                parts[0],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var startDate))
        {
            throw new InvalidOperationException($"Invalid season team membership period '{value}'.");
        }

        DateTime? endDate = null;
        DateTime parsedEndDate = default;
        if (parts[1] != "open" &&
            (!DateTime.TryParseExact(
                parts[1],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedEndDate)))
        {
            throw new InvalidOperationException($"Invalid season team membership period '{value}'.");
        }

        if (parts[1] != "open")
            endDate = parsedEndDate;

        return AssignmentPeriod.Create(startDate, endDate).Value!;
    }
}
