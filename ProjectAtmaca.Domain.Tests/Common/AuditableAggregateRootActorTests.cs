using FluentAssertions;

using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Tests.Common;

public sealed class AuditableAggregateRootActorTests
{
    [Fact]
    public void SetCreatedBy_Should_StoreCanonicalActorIdentity()
    {
        // Arrange
        TestAuditableAggregateRoot aggregate =
            new();

        ActorId actorId =
            ActorId.New();

        // Act
        aggregate.SetCreatedBy(
            actorId);

        ActorId? createdByActorId =
            aggregate.CreatedByActorId;

        // Assert
        createdByActorId
            .Should()
            .Be(actorId);
    }

    [Fact]
    public void SetCreatedBy_Should_RejectEmptyActorIdentity()
    {
        // Arrange
        TestAuditableAggregateRoot aggregate =
            new();

        // Act
        Action act =
            () =>
                aggregate.SetCreatedBy(
                    default(ActorId));

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void SetCreatedBy_Should_NotPermitCreatorReplacement()
    {
        // Arrange
        TestAuditableAggregateRoot aggregate =
            new();

        ActorId originalActorId =
            ActorId.New();

        ActorId replacementActorId =
            ActorId.New();

        aggregate.SetCreatedBy(
            originalActorId);

        // Act
        Action act =
            () =>
                aggregate.SetCreatedBy(
                    replacementActorId);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        aggregate.CreatedByActorId
            .Should()
            .Be(originalActorId);
    }

    [Fact]
    public void MarkAsModified_Should_StoreCanonicalActorIdentity()
    {
        // Arrange
        TestAuditableAggregateRoot aggregate =
            new();

        ActorId actorId =
            ActorId.New();

        // Act
        aggregate.MarkAsModified(
            actorId);

        ActorId? lastModifiedByActorId =
            aggregate.LastModifiedByActorId;

        // Assert
        lastModifiedByActorId
            .Should()
            .Be(actorId);

        aggregate.LastModifiedAtUtc
            .Should()
            .NotBeNull();

        aggregate.LastModifiedAtUtc!
            .Value
            .Kind
            .Should()
            .Be(DateTimeKind.Utc);
    }

    [Fact]
    public void MarkAsModified_Should_RejectEmptyActorIdentity()
    {
        // Arrange
        TestAuditableAggregateRoot aggregate =
            new();

        // Act
        Action act =
            () =>
                aggregate.MarkAsModified(
                    default(ActorId));

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void AuditContract_Should_NotExposeLegacyStringActorStateOrMutators()
    {
        // Arrange
        Type aggregateType =
            typeof(AuditableAggregateRoot);

        System.Reflection.PropertyInfo? legacyCreatedBy =
            aggregateType.GetProperty(
                "CreatedBy");

        System.Reflection.PropertyInfo? legacyLastModifiedBy =
            aggregateType.GetProperty(
                "LastModifiedBy");

        System.Reflection.MethodInfo? legacySetCreatedBy =
            aggregateType.GetMethod(
                nameof(
                    AuditableAggregateRoot.SetCreatedBy),
                new[]
                {
                    typeof(string)
                });

        System.Reflection.MethodInfo? legacyMarkAsModified =
            aggregateType.GetMethod(
                nameof(
                    AuditableAggregateRoot.MarkAsModified),
                new[]
                {
                    typeof(string)
                });

        // Assert
        legacyCreatedBy
            .Should()
            .BeNull();

        legacyLastModifiedBy
            .Should()
            .BeNull();

        legacySetCreatedBy
            .Should()
            .BeNull();

        legacyMarkAsModified
            .Should()
            .BeNull();
    }

    private sealed class TestAuditableAggregateRoot
        : AuditableAggregateRoot
    {
        public TestAuditableAggregateRoot()
            : base(
                Guid.NewGuid())
        {
        }
    }
}
