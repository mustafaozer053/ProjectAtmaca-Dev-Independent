using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Infrastructure.Persistence;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

public sealed class DecisionPersistenceModelTests
{
    [Fact]
    public void Model_Should_MapDecision_ToDecisionsTable()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        // Act
        IEntityType? entityType =
            dbContext.Model.FindEntityType(
                typeof(Decision));

        // Assert
        entityType
            .Should()
            .NotBeNull();

        entityType!
            .GetTableName()
            .Should()
            .Be("Decisions");
    }

    [Fact]
    public void Model_Should_MapDecisionTarget_AsStructuralColumns()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(Decision))!;

        StoreObjectIdentifier table =
            StoreObjectIdentifier.Table(
                "Decisions",
                null);

        // Act
        IComplexProperty? targetProperty =
            entityType.FindComplexProperty(
                nameof(Decision.Target));

        IProperty? targetTypeProperty =
            targetProperty?
                .ComplexType
                .FindProperty("TargetType");

        IProperty? targetIdProperty =
            targetProperty?
                .ComplexType
                .FindProperty("TargetId");

        // Assert
        targetProperty
            .Should()
            .NotBeNull();

        targetTypeProperty
            .Should()
            .NotBeNull();

        targetIdProperty
            .Should()
            .NotBeNull();

        targetTypeProperty!
            .GetColumnName(table)
            .Should()
            .Be("TargetType");

        targetIdProperty!
            .GetColumnName(table)
            .Should()
            .Be("TargetId");
    }

    [Fact]
    public void Model_Should_MapDecisionSnapshot_AsStructuralColumns()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(Decision))!;

        StoreObjectIdentifier table =
            StoreObjectIdentifier.Table(
                "Decisions",
                null);

        // Act
        IComplexProperty? snapshotProperty =
            entityType.FindComplexProperty(
                nameof(Decision.Snapshot));

        // Assert
        snapshotProperty
            .Should()
            .NotBeNull();

        IComplexType snapshotType =
            snapshotProperty!.ComplexType;

        snapshotType
            .FindProperty("ActivityReference")!
            .GetColumnName(table)
            .Should()
            .Be("SnapshotActivityReference");

        snapshotType
            .FindProperty("AtmacaCardId")!
            .GetColumnName(table)
            .Should()
            .Be("SnapshotAtmacaCardId");

        snapshotType
            .FindProperty("Status")!
            .GetColumnName(table)
            .Should()
            .Be("SnapshotStatus");

        snapshotType
            .FindProperty("Condition")!
            .GetColumnName(table)
            .Should()
            .Be("SnapshotConditionCode");

        snapshotType
            .FindProperty("JoinedAt")!
            .GetColumnName(table)
            .Should()
            .Be("SnapshotJoinedAt");

        snapshotType
            .FindProperty("LeftAt")!
            .GetColumnName(table)
            .Should()
            .Be("SnapshotLeftAt");
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
