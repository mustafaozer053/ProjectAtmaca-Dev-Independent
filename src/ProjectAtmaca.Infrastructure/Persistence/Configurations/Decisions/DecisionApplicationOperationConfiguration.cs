using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.Decisions;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Configurations.Decisions;

public sealed class DecisionApplicationOperationConfiguration
    : IEntityTypeConfiguration<DecisionApplicationOperation>
{
    public void Configure(
        EntityTypeBuilder<DecisionApplicationOperation> builder)
    {
        builder.ToTable(
            "DecisionApplicationOperations");

        builder.HasKey(
            operation => operation.OperationId);

        builder.Property(
                operation => operation.OperationId)
            .HasConversion(
                operationId => operationId.Value,
                value =>
                    DecisionApplicationOperationId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                operation => operation.DecisionId)
            .HasConversion(
                decisionId => decisionId.Value,
                value =>
                    DecisionId.From(
                        value));

        builder.Property(
                operation => operation.DecisionRevision)
            .HasConversion(
                revision => revision.Value,
                value =>
                    DecisionRevision.From(
                        value));

        builder.Property(
                operation => operation.AppliedAtUtc);
    }
}
