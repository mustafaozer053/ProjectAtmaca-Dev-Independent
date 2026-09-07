using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Configurations.Security;

public sealed class ActorPermissionGrantConfiguration
    : IEntityTypeConfiguration<ActorPermissionGrant>
{
    private const string ExactPermissionCollation =
        "Latin1_General_100_BIN2";

    public void Configure(
        EntityTypeBuilder<ActorPermissionGrant> builder)
    {
        builder.ToTable(
            "ActorPermissionGrants");

        builder.HasKey(
            grant =>
                grant.Id);

        builder.Property(
                grant =>
                    grant.Id)
            .ValueGeneratedOnAdd();

        builder.Property(
                grant =>
                    grant.ActorId)
            .HasConversion(
                actorId =>
                    actorId.Value,
                value =>
                    ActorId.From(
                        value))
            .IsRequired();

        builder.Property(
                grant =>
                    grant.PermissionCode)
            .HasMaxLength(255)
            .UseCollation(
                ExactPermissionCollation)
            .IsRequired();

        builder.Property<int>(
                "PermissionCodeByteLength")
            .HasComputedColumnSql(
                "DATALENGTH([PermissionCode])",
                stored: true);

        builder.HasIndex(
                nameof(
                    ActorPermissionGrant.ActorId),
                nameof(
                    ActorPermissionGrant.PermissionCode),
                "PermissionCodeByteLength")
            .IsUnique();
    }
}