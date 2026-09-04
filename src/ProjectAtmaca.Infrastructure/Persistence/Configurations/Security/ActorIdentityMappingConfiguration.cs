using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Configurations.Security;

public sealed class ActorIdentityMappingConfiguration
    : IEntityTypeConfiguration<ActorIdentityMapping>
{
    private const string ExactIdentityCollation =
        "Latin1_General_100_BIN2";

    public void Configure(
        EntityTypeBuilder<ActorIdentityMapping> builder)
    {
        builder.ToTable(
            "ActorIdentityMappings");

        builder.HasKey(
            mapping =>
                mapping.Id);

        builder.Property(
                mapping =>
                    mapping.Id)
            .ValueGeneratedOnAdd();

        builder.Property(
                mapping =>
                    mapping.Issuer)
            .HasMaxLength(512)
            .UseCollation(
                ExactIdentityCollation)
            .IsRequired();

        builder.Property<int>(
                "IssuerByteLength")
            .HasComputedColumnSql(
                "DATALENGTH([Issuer])",
                stored: true);

        builder.Property(
                mapping =>
                    mapping.Subject)
            .HasMaxLength(255)
            .UseCollation(
                ExactIdentityCollation)
            .IsRequired();

        builder.Property<int>(
                "SubjectByteLength")
            .HasComputedColumnSql(
                "DATALENGTH([Subject])",
                stored: true);

        builder.Property(
                mapping =>
                    mapping.ActorId)
            .HasConversion(
                actorId =>
                    actorId.Value,
                value =>
                    ActorId.From(
                        value))
            .IsRequired();

        builder.HasIndex(
                nameof(
                    ActorIdentityMapping.Issuer),
                "IssuerByteLength",
                nameof(
                    ActorIdentityMapping.Subject),
                "SubjectByteLength")
            .IsUnique();

        builder.HasIndex(
            mapping =>
                mapping.ActorId);
    }
}