using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Infrastructure.Persistence;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

public sealed class DecisionApplicationPersistenceModelTests
{
    [Fact]
    public void Model_Should_MapDecisionApplication_ToDecisionApplicationsTable()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        // Act
        IEntityType? entityType =
            dbContext.Model.FindEntityType(
                typeof(DecisionApplication));

        // Assert
        entityType
            .Should()
            .NotBeNull();

        entityType!
            .GetTableName()
            .Should()
            .Be("DecisionApplications");
    }

    [Fact]
    public void Model_Should_MapDecisionApplication_ProvenanceColumns()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(DecisionApplication))!;

        StoreObjectIdentifier table =
            StoreObjectIdentifier.Table(
                "DecisionApplications",
                null);

        IComplexProperty targetProperty =
            entityType.FindComplexProperty(
                nameof(DecisionApplication.Target))!;

        // Act
        IProperty? decisionIdProperty =
            entityType.FindProperty(
                nameof(DecisionApplication.DecisionId));

        IProperty? appliedRevisionProperty =
            entityType.FindProperty(
                nameof(DecisionApplication.AppliedDecisionRevision));

        IProperty? appliedAtUtcProperty =
            entityType.FindProperty(
                nameof(DecisionApplication.AppliedAtUtc));

        IProperty? targetTypeProperty =
            targetProperty.ComplexType.FindProperty(
                "TargetType");

        IProperty? targetIdProperty =
            targetProperty.ComplexType.FindProperty(
                "TargetId");

        // Assert
        decisionIdProperty
            .Should()
            .NotBeNull();

        decisionIdProperty!
            .GetColumnName(table)
            .Should()
            .Be("DecisionId");

        targetTypeProperty
            .Should()
            .NotBeNull();

        targetTypeProperty!
            .GetColumnName(table)
            .Should()
            .Be("TargetType");

        targetIdProperty
            .Should()
            .NotBeNull();

        targetIdProperty!
            .GetColumnName(table)
            .Should()
            .Be("TargetId");

        appliedRevisionProperty
            .Should()
            .NotBeNull();

        appliedRevisionProperty!
            .GetColumnName(table)
            .Should()
            .Be("AppliedDecisionRevision");

        appliedAtUtcProperty
            .Should()
            .NotBeNull();

        appliedAtUtcProperty!
            .GetColumnName(table)
            .Should()
            .Be("AppliedAtUtc");
    }

    [Fact]
    public void Model_Should_Not_DuplicateDecisionState_InDecisionApplication()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(DecisionApplication))!;

        // Act
        IReadOnlyList<string> propertyNames =
            entityType
                .GetProperties()
                .Select(property => property.Name)
                .ToList();

        IReadOnlyList<string> complexPropertyNames =
            entityType
                .GetComplexProperties()
                .Select(property => property.Name)
                .ToList();

        // Assert
        propertyNames
            .Should()
            .NotContain("Snapshot");

        propertyNames
            .Should()
            .NotContain("Effect");

        propertyNames
            .Should()
            .NotContain("CurrentDecisionRevision");

        propertyNames
            .Should()
            .NotContain("CurrentDecisionStatus");

        propertyNames
            .Should()
            .NotContain("SupersededByDecisionId");

        complexPropertyNames
            .Should()
            .NotContain("Snapshot");

        complexPropertyNames
            .Should()
            .NotContain("Effect");
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
