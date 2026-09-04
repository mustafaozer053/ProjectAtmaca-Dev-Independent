using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Security;

public sealed class ActorIdentityMappingPersistenceModelTests
{
    private const string ExactIdentityCollation =
        "Latin1_General_100_BIN2";

    [Fact]
    public void Model_Should_MapActorIdentityMapping_ToDedicatedTable()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        // Act
        IEntityType? entityType =
            GetDesignTimeModel(dbContext).FindEntityType(
                typeof(ActorIdentityMapping));

        // Assert
        entityType
            .Should()
            .NotBeNull();

        entityType!
            .GetTableName()
            .Should()
            .Be("ActorIdentityMappings");

        entityType.FindProperty(
                nameof(ActorIdentityMapping.Id))
            .Should()
            .NotBeNull();

        entityType.FindProperty(
                nameof(ActorIdentityMapping.Issuer))
            .Should()
            .NotBeNull();

        entityType.FindProperty(
                nameof(ActorIdentityMapping.Subject))
            .Should()
            .NotBeNull();

        entityType.FindProperty(
                nameof(ActorIdentityMapping.ActorId))
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void Model_Should_UseSurrogateKey_AndExactUniqueExternalIdentityKey()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetDesignTimeModel(dbContext).FindEntityType(
                typeof(ActorIdentityMapping))!;

        // Act
        IKey? primaryKey =
            entityType.FindPrimaryKey();

        IIndex? externalIdentityIndex =
            entityType
                .GetIndexes()
                .SingleOrDefault(
                    index =>
                        index.Properties
                            .Select(
                                property =>
                                    property.Name)
                            .SequenceEqual(
                                new[]
                                {
                                    nameof(
                                        ActorIdentityMapping.Issuer),
                                    "IssuerByteLength",
                                    nameof(
                                        ActorIdentityMapping.Subject),
                                    "SubjectByteLength"
                                }));

        // Assert
        primaryKey
            .Should()
            .NotBeNull();

        primaryKey!
            .Properties
            .Select(
                property =>
                    property.Name)
            .Should()
            .Equal(
                nameof(
                    ActorIdentityMapping.Id));

        externalIdentityIndex
            .Should()
            .NotBeNull();

        externalIdentityIndex!
            .IsUnique
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Model_Should_StoreExactBoundedExternalIdentityComponents()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetDesignTimeModel(dbContext).FindEntityType(
                typeof(ActorIdentityMapping))!;

        // Act
        IProperty issuerProperty =
            entityType.FindProperty(
                nameof(
                    ActorIdentityMapping.Issuer))!;

        IProperty subjectProperty =
            entityType.FindProperty(
                nameof(
                    ActorIdentityMapping.Subject))!;

        // Assert
        issuerProperty.IsNullable
            .Should()
            .BeFalse();

        issuerProperty
            .GetMaxLength()
            .Should()
            .Be(512);

        issuerProperty
            .GetCollation()
            .Should()
            .Be(ExactIdentityCollation);

        subjectProperty.IsNullable
            .Should()
            .BeFalse();

        subjectProperty
            .GetMaxLength()
            .Should()
            .Be(255);

        subjectProperty
            .GetCollation()
            .Should()
            .Be(ExactIdentityCollation);
    }

    [Fact]
    public void Model_Should_UseComputedByteLengths_ForExactStringIdentity()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetDesignTimeModel(dbContext).FindEntityType(
                typeof(ActorIdentityMapping))!;

        // Act
        IProperty? issuerByteLengthProperty =
            entityType.FindProperty(
                "IssuerByteLength");

        IProperty? subjectByteLengthProperty =
            entityType.FindProperty(
                "SubjectByteLength");

        // Assert
        issuerByteLengthProperty
            .Should()
            .NotBeNull();

        issuerByteLengthProperty!
            .ClrType
            .Should()
            .Be(typeof(int));

        issuerByteLengthProperty
            .GetComputedColumnSql()
            .Should()
            .Be("DATALENGTH([Issuer])");

        subjectByteLengthProperty
            .Should()
            .NotBeNull();

        subjectByteLengthProperty!
            .ClrType
            .Should()
            .Be(typeof(int));

        subjectByteLengthProperty
            .GetComputedColumnSql()
            .Should()
            .Be("DATALENGTH([Subject])");
    }
    [Fact]
    public void Model_Should_PersistCanonicalActorId_AndAllowMultipleMappings()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetDesignTimeModel(dbContext).FindEntityType(
                typeof(ActorIdentityMapping))!;

        // Act
        IProperty actorIdProperty =
            entityType.FindProperty(
                nameof(
                    ActorIdentityMapping.ActorId))!;

        IIndex? actorIdIndex =
            entityType
                .GetIndexes()
                .SingleOrDefault(
                    index =>
                        index.Properties
                            .Select(
                                property =>
                                    property.Name)
                            .SequenceEqual(
                                new[]
                                {
                                    nameof(
                                        ActorIdentityMapping.ActorId)
                                }));

        // Assert
        actorIdProperty.IsNullable
            .Should()
            .BeFalse();

        actorIdProperty
            .GetValueConverter()
            .Should()
            .NotBeNull();

        actorIdProperty
            .GetValueConverter()!
            .ProviderClrType
            .Should()
            .Be(typeof(Guid));

        actorIdIndex
            .Should()
            .NotBeNull();

        actorIdIndex!
            .IsUnique
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Model_Should_NotReferencePersonOrAtmacaCardPersistence()
    {
        // Arrange
        using ProjectAtmacaDbContext dbContext =
            CreateDbContext();

        IEntityType entityType =
            GetDesignTimeModel(dbContext).FindEntityType(
                typeof(ActorIdentityMapping))!;

        // Act
        IReadOnlyList<IForeignKey> foreignKeys =
            entityType
                .GetForeignKeys()
                .ToList();

        // Assert
        foreignKeys
            .Should()
            .BeEmpty();
    }

    private static IModel GetDesignTimeModel(
        ProjectAtmacaDbContext dbContext)
    {
        return dbContext
            .GetService<IDesignTimeModel>()
            .Model;
    }
    private static ProjectAtmacaDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ProjectAtmaca_ActorIdentityModelTests;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;")
                .Options;

        return new ProjectAtmacaDbContext(
            options);
    }
}