using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Infrastructure.Persistence;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

public sealed class DecisionApplicationOperationPersistenceModelTests
{
    [Fact]
    public void Model_Should_MapDecisionApplicationOperation_ToDecisionApplicationOperationsTable()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        // Act
        IEntityType? entityType =
            dbContext.Model.FindEntityType(
                typeof(DecisionApplicationOperation));

        // Assert
        entityType
            .Should()
            .NotBeNull();

        entityType!
            .GetTableName()
            .Should()
            .Be("DecisionApplicationOperations");
    }

    [Fact]
    public void Model_Should_MapDecisionApplicationOperation_SemanticIdentityColumns()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(DecisionApplicationOperation))!;

        StoreObjectIdentifier table =
            StoreObjectIdentifier.Table(
                "DecisionApplicationOperations",
                null);

        // Act
        IProperty? operationIdProperty =
            entityType.FindProperty(
                nameof(DecisionApplicationOperation.OperationId));

        IProperty? decisionIdProperty =
            entityType.FindProperty(
                nameof(DecisionApplicationOperation.DecisionId));

        IProperty? decisionRevisionProperty =
            entityType.FindProperty(
                nameof(DecisionApplicationOperation.DecisionRevision));

        IProperty? appliedAtUtcProperty =
            entityType.FindProperty(
                nameof(DecisionApplicationOperation.AppliedAtUtc));

        // Assert
        operationIdProperty
            .Should()
            .NotBeNull();

        operationIdProperty!
            .GetColumnName(table)
            .Should()
            .Be("OperationId");

        decisionIdProperty
            .Should()
            .NotBeNull();

        decisionIdProperty!
            .GetColumnName(table)
            .Should()
            .Be("DecisionId");

        decisionRevisionProperty
            .Should()
            .NotBeNull();

        decisionRevisionProperty!
            .GetColumnName(table)
            .Should()
            .Be("DecisionRevision");

        appliedAtUtcProperty
            .Should()
            .NotBeNull();

        appliedAtUtcProperty!
            .GetColumnName(table)
            .Should()
            .Be("AppliedAtUtc");
    }

    [Fact]
    public void Model_Should_UseOperationId_AsPrimaryKey()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(DecisionApplicationOperation))!;

        // Act
        IKey? primaryKey =
            entityType.FindPrimaryKey();

        // Assert
        primaryKey
            .Should()
            .NotBeNull();

        primaryKey!
            .Properties
            .Should()
            .ContainSingle();

        primaryKey.Properties[0].Name
            .Should()
            .Be(
                nameof(
                    DecisionApplicationOperation.OperationId));
    }

    [Fact]
    public void Model_Should_Not_UseDecisionRevision_AsUniqueApplicationIdentity()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(DecisionApplicationOperation))!;

        // Act
        IReadOnlyList<IIndex> indexes =
            entityType
                .GetIndexes()
                .ToList();

        // Assert
        indexes
            .Where(index => index.IsUnique)
            .Should()
            .NotContain(
                index =>
                    index.Properties
                        .Select(property => property.Name)
                        .SequenceEqual(
                            new[]
                            {
                                nameof(
                                    DecisionApplicationOperation.DecisionId),
                                nameof(
                                    DecisionApplicationOperation.DecisionRevision)
                            }));
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
