using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Configurations.Decisions;

public sealed class DecisionApplicationConfiguration
    : IEntityTypeConfiguration<DecisionApplication>
{
    public void Configure(
        EntityTypeBuilder<DecisionApplication> builder)
    {
        builder.ToTable(
            "DecisionApplications");

        builder.Property(
            x => x.AppliedDecisionRevision)
                .HasConversion(
                    revision => revision.Value,
                    value => DecisionRevision.From(value))
                .HasColumnName(
                    "AppliedDecisionRevision")
                .IsRequired();

        builder.HasKey(
            "Id");

        builder.Property(
                "Id")
            .HasColumnName(
                "Id")
            .ValueGeneratedNever();

        builder.Ignore(
            x => x.DecisionApplicationId);

        builder.Property(
                x => x.DecisionId)
            .HasConversion(
                decisionId => decisionId.Value,
                value => DecisionId.From(value))
            .HasColumnName(
                "DecisionId")
            .IsRequired();

        // TargetType/TargetId INCLUDE columns are migration-managed: EF 8 rejects complex-property paths.
        // See docs/decision-history-sql-maliyeti.md and DecisionHistoryIndexMigrationTests.
        builder.HasIndex(nameof(DecisionApplication.DecisionId), nameof(DecisionApplication.AppliedAtUtc), nameof(DecisionApplication.Id))
            .IsDescending(false, true, true)
            .IncludeProperties(nameof(DecisionApplication.AppliedDecisionRevision))
            .HasDatabaseName("IX_DecisionApplications_Decision_AppliedAt_Id");

        builder.ComplexProperty(
            x => x.Target,
            target =>
            {
                target.Property(x => x.TargetType)
                    .HasConversion<int>()
                    .HasColumnName("TargetType")
                    .IsRequired();

                target.Property(x => x.TargetId)
                    .HasColumnName("TargetId")
                    .IsRequired();
            });
    }
}
