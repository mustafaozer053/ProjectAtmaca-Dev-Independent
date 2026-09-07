using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Security;

public sealed class ActorPermissionGrantPersistenceModelTests
{
    private const string ExactPermissionCollation =
        "Latin1_General_100_BIN2";

    private const int PermissionCodeMaxLength =
        255;

    [Fact]
    public void Create_Should_PreserveExactActorAndPermission()
    {
        ActorId actorId =
            ActorId.New();

        Permission permission =
            Permissions.Participations.MarkPresent;

        ActorPermissionGrant grant =
            ActorPermissionGrant.Create(
                actorId,
                permission);

        grant.Id.Should().Be(0);
        grant.ActorId.Should().Be(actorId);
        grant.PermissionCode.Should().Be(
            permission.Code);
    }

    [Fact]
    public void Create_Should_RejectEmptyActorId()
    {
        ActorId emptyActorId =
            default;

        Action action =
            () => ActorPermissionGrant.Create(
                emptyActorId,
                Permissions.Participations.MarkPresent);

        action.Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_RejectNullPermission()
    {
        Action action =
            () => ActorPermissionGrant.Create(
                ActorId.New(),
                null!);

        action.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void Model_Should_UseDedicatedTableAndGeneratedSurrogateKey()
    {
        IEntityType entityType =
            GetEntityType();

        entityType.GetTableName()
            .Should()
            .Be("ActorPermissionGrants");

        IKey? primaryKey =
            entityType.FindPrimaryKey();

        primaryKey.Should()
            .NotBeNull();

        primaryKey!.Properties
            .Select(property => property.Name)
            .Should()
            .Equal(
                nameof(ActorPermissionGrant.Id));

        IProperty? idProperty =
            entityType.FindProperty(
                nameof(ActorPermissionGrant.Id));

        idProperty.Should()
            .NotBeNull();

        idProperty!.ClrType
            .Should()
            .Be(typeof(long));

        idProperty.IsNullable
            .Should()
            .BeFalse();

        idProperty.ValueGenerated
            .Should()
            .Be(ValueGenerated.OnAdd);
    }

    [Fact]
    public void Model_Should_StoreExactBoundedPermissionIdentity()
    {
        IEntityType entityType =
            GetEntityType();

        IProperty? actorIdProperty =
            entityType.FindProperty(
                nameof(ActorPermissionGrant.ActorId));

        actorIdProperty.Should()
            .NotBeNull();

        actorIdProperty!.ClrType
            .Should()
            .Be(typeof(ActorId));

        actorIdProperty.IsNullable
            .Should()
            .BeFalse();

        actorIdProperty.GetValueConverter()
            .Should()
            .NotBeNull();

        actorIdProperty.GetValueConverter()!
            .ProviderClrType
            .Should()
            .Be(typeof(Guid));

        IProperty? permissionCodeProperty =
            entityType.FindProperty(
                nameof(ActorPermissionGrant.PermissionCode));

        permissionCodeProperty.Should()
            .NotBeNull();

        permissionCodeProperty!.ClrType
            .Should()
            .Be(typeof(string));

        permissionCodeProperty.IsNullable
            .Should()
            .BeFalse();

        permissionCodeProperty.GetMaxLength()
            .Should()
            .Be(PermissionCodeMaxLength);

        permissionCodeProperty.GetCollation()
            .Should()
            .Be(ExactPermissionCollation);

        IProperty? byteLengthProperty =
            entityType.FindProperty(
                "PermissionCodeByteLength");

        byteLengthProperty.Should()
            .NotBeNull();

        byteLengthProperty!.ClrType
            .Should()
            .Be(typeof(int));

        byteLengthProperty.IsNullable
            .Should()
            .BeFalse();

        byteLengthProperty.IsShadowProperty()
            .Should()
            .BeTrue();

        byteLengthProperty.GetComputedColumnSql()
            .Should()
            .Be("DATALENGTH([PermissionCode])");

        byteLengthProperty.GetIsStored()
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Model_Should_EnforceExactActorPermissionUniquenessWithoutForeignKeys()
    {
        IEntityType entityType =
            GetEntityType();

        IIndex[] indexes =
            entityType.GetIndexes()
                .ToArray();

        indexes.Should()
            .ContainSingle();

        IIndex exactIdentityIndex =
            indexes.Single();

        exactIdentityIndex.IsUnique
            .Should()
            .BeTrue();

        exactIdentityIndex.Properties
            .Select(property => property.Name)
            .Should()
            .Equal(
                nameof(ActorPermissionGrant.ActorId),
                nameof(ActorPermissionGrant.PermissionCode),
                "PermissionCodeByteLength");

        entityType.GetForeignKeys()
            .Should()
            .BeEmpty(
                "the relational model has no canonical Actors table");

        entityType.GetNavigations()
            .Should()
            .BeEmpty();
    }

    private static IEntityType GetEntityType()
    {
        DbContextOptions<ProjectAtmacaDbContext> options =
            new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ProjectAtmaca_ActorPermissionGrantModelTests;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True")
                .Options;

        using var context =
            new ProjectAtmacaDbContext(
                options);

        IModel model =
            context.GetService<IDesignTimeModel>()
                .Model;

        IEntityType? entityType =
            model.FindEntityType(
                typeof(ActorPermissionGrant));

        entityType.Should()
            .NotBeNull(
                "actor permission grants must participate in the relational model");

        return entityType!;
    }
}