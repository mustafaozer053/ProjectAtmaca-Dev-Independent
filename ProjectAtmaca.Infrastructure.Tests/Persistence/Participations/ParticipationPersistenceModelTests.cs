using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

public sealed class ParticipationPersistenceModelTests
{
    [Fact]
    public void Model_Should_Map_Participation_ToExpectedTable()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        entityType.GetTableName()
            .Should()
            .Be("Participations");
    }

    [Fact]
    public void Model_Should_Configure_Id_AsPrimaryKey()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IKey? primaryKey =
            entityType.FindPrimaryKey();

        primaryKey.Should().NotBeNull();

        primaryKey!.Properties
            .Should()
            .ContainSingle();

        primaryKey.Properties.Single().Name
            .Should()
            .Be("Id");
    }

    [Fact]
    public void Model_Should_Configure_AtmacaCardId_WithConverter()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IProperty? property =
            entityType.FindProperty(
                nameof(Participation.AtmacaCardId));

        property.Should().NotBeNull();

        property!.IsNullable
            .Should()
            .BeFalse();

        property.GetValueConverter()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void Model_Should_Configure_ActivityReference_WithConverter()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IProperty? property =
            entityType.FindProperty(
                nameof(Participation.ActivityReference));

        property.Should().NotBeNull();

        property!.IsNullable
            .Should()
            .BeFalse();

        property.GetValueConverter()
            .Should()
            .NotBeNull();

        property.GetMaxLength()
            .Should()
            .Be(64);
    }

    [Fact]
    public void Model_Should_Have_Unique_SubjectContext_Index()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IIndex? index =
            entityType.GetIndexes()
                .SingleOrDefault(index =>
                    index.Properties
                        .Select(property => property.Name)
                        .SequenceEqual(
                        [
                            nameof(Participation.AtmacaCardId),
                            nameof(Participation.ActivityReference)
                        ]));

        index.Should().NotBeNull();

        index!.IsUnique
            .Should()
            .BeTrue();

        index.GetDatabaseName()
            .Should()
            .Be(
                "UX_Participations_AtmacaCard_Activity");
    }

    [Fact]
    public void Model_Should_Configure_Status_AsRequired()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IProperty? property =
            entityType.FindProperty(
                nameof(Participation.Status));

        property.Should().NotBeNull();

        property!.IsNullable
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Model_Should_Configure_Condition_AsOptional()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IProperty? property =
            entityType.FindProperty(
                nameof(Participation.Condition));

        property.Should().NotBeNull();

        property!.IsNullable
            .Should()
            .BeTrue();

        property.GetValueConverter()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void Model_Should_Configure_TemporalFacts_AsOptional()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IProperty? joinedAt =
            entityType.FindProperty(
                nameof(Participation.JoinedAt));

        IProperty? leftAt =
            entityType.FindProperty(
                nameof(Participation.LeftAt));

        joinedAt.Should().NotBeNull();
        leftAt.Should().NotBeNull();

        joinedAt!.IsNullable
            .Should()
            .BeTrue();

        leftAt!.IsNullable
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Model_Should_Configure_Note_AsOptional_WithMaximumLength()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IProperty? property =
            entityType.FindProperty(
                nameof(Participation.Note));

        property.Should().NotBeNull();

        property!.IsNullable
            .Should()
            .BeTrue();

        property.GetMaxLength()
            .Should()
            .Be(ParticipationNote.MaxLength);

        property.GetValueConverter()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void Model_Should_Configure_RowVersion_AsConcurrencyToken()
    {
        using ProjectAtmacaDbContext context =
            CreateContext();

        IEntityType entityType =
            GetParticipationEntityType(context);

        IProperty? property =
            entityType.FindProperty(
                "RowVersion");

        property.Should().NotBeNull();

        property!.IsConcurrencyToken
            .Should()
            .BeTrue();

        property.ValueGenerated
            .Should()
            .Be(ValueGenerated.OnAddOrUpdate);
    }

    private static ProjectAtmacaDbContext CreateContext()
    {
        DbContextOptions<ProjectAtmacaDbContext> options =
            new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ProjectAtmaca_ModelValidation;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True")
                .Options;

        return new ProjectAtmacaDbContext(
            options);
    }

    private static IEntityType GetParticipationEntityType(
        ProjectAtmacaDbContext context)
    {
        return context.Model
            .FindEntityType(typeof(Participation))
            ?? throw new InvalidOperationException(
                "Participation entity type was not found " +
                "in the EF Core model.");
    }
}
