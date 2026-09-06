using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

public sealed class ParticipationAuditPersistenceModelTests
{
    [Fact]
    public void Model_Should_MapCanonicalActorAuditColumns()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType? entityType =
            dbContext.Model.FindEntityType(
                typeof(Participation));

        entityType
            .Should()
            .NotBeNull();

        entityType!
            .GetTableName()
            .Should()
            .Be("Participations");

        StoreObjectIdentifier table =
            StoreObjectIdentifier.Table(
                entityType.GetTableName()!,
                entityType.GetSchema());

        // Act
        IProperty? createdByActorIdProperty =
            entityType.FindProperty(
                nameof(
                    Participation.CreatedByActorId));

        IProperty? lastModifiedByActorIdProperty =
            entityType.FindProperty(
                nameof(
                    Participation.LastModifiedByActorId));

        // Assert
        AssertCanonicalActorProperty(
            createdByActorIdProperty,
            nameof(
                Participation.CreatedByActorId),
            table);

        AssertCanonicalActorProperty(
            lastModifiedByActorIdProperty,
            nameof(
                Participation.LastModifiedByActorId),
            table);

        IReadOnlyList<string> foreignKeyPropertyNames =
            entityType
                .GetForeignKeys()
                .SelectMany(
                    foreignKey =>
                        foreignKey.Properties)
                .Select(
                    property =>
                        property.Name)
                .ToList();

        foreignKeyPropertyNames
            .Should()
            .NotContain(
                nameof(
                    Participation.CreatedByActorId));

        foreignKeyPropertyNames
            .Should()
            .NotContain(
                nameof(
                    Participation.LastModifiedByActorId));
    }

    [Fact]
    public void Model_Should_PreserveLegacyTextAuditColumns_ForBackfillCompatibility()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(Participation))!;

        StoreObjectIdentifier table =
            StoreObjectIdentifier.Table(
                entityType.GetTableName()!,
                entityType.GetSchema());

        // Act
        IProperty? createdByProperty =
            entityType.FindProperty(
                "CreatedBy");

        IProperty? lastModifiedByProperty =
            entityType.FindProperty(
                "LastModifiedBy");

        // Assert
        AssertLegacyTextAuditProperty(
            createdByProperty,
            "CreatedBy",
            table);

        AssertLegacyTextAuditProperty(
            lastModifiedByProperty,
            "LastModifiedBy",
            table);
    }

    private static void AssertCanonicalActorProperty(
        IProperty? property,
        string expectedColumnName,
        StoreObjectIdentifier table)
    {
        property
            .Should()
            .NotBeNull(
                "canonical actor audit identity must be persisted");

        property!
            .ClrType
            .Should()
            .Be(
                typeof(ActorId?));

        property
            .IsShadowProperty()
            .Should()
            .BeFalse();

        property
            .IsNullable
            .Should()
            .BeTrue(
                "existing rows require an additive backfill seam");

        property
            .GetColumnName(table)
            .Should()
            .Be(
                expectedColumnName);

        property
            .GetColumnType()
            .Should()
            .Be(
                "uniqueidentifier");
    }

    private static void AssertLegacyTextAuditProperty(
        IProperty? property,
        string expectedColumnName,
        StoreObjectIdentifier table)
    {
        property
            .Should()
            .NotBeNull(
                "legacy audit data must remain available during migration");

        property!
            .ClrType
            .Should()
            .Be(
                typeof(string));

        property
            .IsShadowProperty()
            .Should()
            .BeTrue();

        property
            .IsNullable
            .Should()
            .BeTrue();

        property
            .GetMaxLength()
            .Should()
            .Be(
                200);

        property
            .GetColumnName(table)
            .Should()
            .Be(
                expectedColumnName);

        property
            .GetColumnType()
            .Should()
            .Be(
                "nvarchar(200)");
    }

    private static ProjectAtmacaDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ProjectAtmaca_ModelTests;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new ProjectAtmacaDbContext(
            options);
    }
}
