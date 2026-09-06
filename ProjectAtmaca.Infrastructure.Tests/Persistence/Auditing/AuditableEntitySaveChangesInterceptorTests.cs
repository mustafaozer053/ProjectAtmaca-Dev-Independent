using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Auditing;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Auditing;

public sealed class AuditableEntitySaveChangesInterceptorTests
{
    [Fact]
    public async Task
        SaveChangesAsync_Should_SetCanonicalCreator_ForAddedAuditableAggregate()
    {
        // Arrange
        ActorId actorId =
            ActorId.New();

        var currentActor =
            FakeCurrentActor.For(
                actorId);

        await using ProjectAtmacaDbContext context =
            CreateContext(
                currentActor);

        Participation participation =
            CreateParticipation();

        context
            .Set<Participation>()
            .Add(
                participation);

        // Act
        await context.SaveChangesAsync(
            CancellationToken.None);

        // Assert
        participation.CreatedByActorId
            .Should()
            .Be(
                actorId);

        participation.LastModifiedByActorId
            .Should()
            .BeNull();

        participation.LastModifiedAtUtc
            .Should()
            .BeNull();

        currentActor.AccessCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        SaveChangesAsync_Should_PreserveCreatorAndSetModifier_ForModifiedAuditableAggregate()
    {
        // Arrange
        ActorId originalCreatorActorId =
            ActorId.New();

        ActorId currentActorId =
            ActorId.New();

        var currentActor =
            FakeCurrentActor.For(
                currentActorId);

        await using ProjectAtmacaDbContext context =
            CreateContext(
                currentActor);

        Participation participation =
            CreateParticipation();

        participation.SetCreatedBy(
            originalCreatorActorId);

        context.Attach(
            participation);

        context
            .Entry(
                participation)
            .State =
                EntityState.Modified;

        // Act
        await context.SaveChangesAsync(
            CancellationToken.None);

        // Assert
        participation.CreatedByActorId
            .Should()
            .Be(
                originalCreatorActorId);

        participation.LastModifiedByActorId
            .Should()
            .Be(
                currentActorId);

        participation.LastModifiedAtUtc
            .Should()
            .NotBeNull();

        currentActor.AccessCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        SaveChangesAsync_Should_NotResolveCurrentActor_WhenNoAuditableChangeExists()
    {
        // Arrange
        var currentActor =
            FakeCurrentActor.Throwing();

        await using ProjectAtmacaDbContext context =
            CreateContext(
                currentActor);

        // Act
        Func<Task> act =
            async () =>
                await context.SaveChangesAsync(
                    CancellationToken.None);

        // Assert
        await act
            .Should()
            .NotThrowAsync();

        currentActor.AccessCount
            .Should()
            .Be(0);
    }

    private static ProjectAtmacaDbContext CreateContext(
        ICurrentActor currentActor)
    {
        var auditInterceptor =
            new AuditableEntitySaveChangesInterceptor(
                currentActor);

        var suppressingInterceptor =
            new SuppressingSaveChangesInterceptor();

        DbContextOptions<ProjectAtmacaDbContext> options =
            new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;" +
                    "Database=ProjectAtmaca_AuditInterceptorContract;" +
                    "Trusted_Connection=True;" +
                    "MultipleActiveResultSets=true")
                .AddInterceptors(
                    auditInterceptor,
                    suppressingInterceptor)
                .Options;

        return new ProjectAtmacaDbContext(
            options);
    }

    private static Participation CreateParticipation()
    {
        var creationResult =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New());

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        return creationResult.Value!;
    }

    private sealed class FakeCurrentActor
        : ICurrentActor
    {
        private readonly ActorId _actorId;
        private readonly bool _throwOnAccess;

        private FakeCurrentActor(
            ActorId actorId,
            bool throwOnAccess)
        {
            _actorId = actorId;
            _throwOnAccess = throwOnAccess;
        }

        public int AccessCount { get; private set; }

        public ActorId ActorId
        {
            get
            {
                AccessCount++;

                if (_throwOnAccess)
                {
                    throw new InvalidOperationException(
                        "Current actor must not be resolved.");
                }

                return _actorId;
            }
        }

        public static FakeCurrentActor For(
            ActorId actorId)
        {
            return new FakeCurrentActor(
                actorId,
                throwOnAccess: false);
        }

        public static FakeCurrentActor Throwing()
        {
            return new FakeCurrentActor(
                default,
                throwOnAccess: true);
        }
    }

    private sealed class SuppressingSaveChangesInterceptor
        : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                InterceptionResult<int>.SuppressWithResult(
                    0));
        }
    }
}
