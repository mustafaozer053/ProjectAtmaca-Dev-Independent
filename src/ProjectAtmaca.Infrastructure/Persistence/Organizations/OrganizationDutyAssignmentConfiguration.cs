using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Assignments;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Infrastructure.Persistence.Organizations;

public sealed class OrganizationDutyAssignmentConfiguration
    : IEntityTypeConfiguration<OrganizationDutyAssignment>
{
    public void Configure(EntityTypeBuilder<OrganizationDutyAssignment> builder)
    {
        builder.ToTable("OrganizationDutyAssignments");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).ValueGeneratedNever();
        builder.Property(assignment => assignment.AtmacaCardId).IsRequired();
        builder.Property(assignment => assignment.OrganizationId).IsRequired();
        builder.Property(assignment => assignment.Title)
            .HasConversion(title => title.Value, value => AssignmentTitle.Create(value).Value!)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(assignment => assignment.StartDate)
            .HasColumnType("date")
            .IsRequired();
        builder.Property(assignment => assignment.EndDate)
            .HasColumnType("date");
        builder.HasIndex(assignment => new
        {
            assignment.OrganizationId,
            assignment.AtmacaCardId,
            assignment.StartDate
        });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(assignment => assignment.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AtmacaCard>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AtmacaCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(assignment => assignment.CreatedAtUtc).IsRequired();
        builder.Property(assignment => assignment.CreatedByActorId)
            .HasConversion(
                actorId => actorId.HasValue ? actorId.Value.Value : (Guid?)null,
                value => value.HasValue ? ActorId.From(value.Value) : null)
            .IsRequired(false);
        builder.Property(assignment => assignment.LastModifiedAtUtc).IsRequired(false);
        builder.Property(assignment => assignment.LastModifiedByActorId)
            .HasConversion(
                actorId => actorId.HasValue ? actorId.Value.Value : (Guid?)null,
                value => value.HasValue ? ActorId.From(value.Value) : null)
            .IsRequired(false);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(assignment => assignment.DomainEvents);
    }
}
