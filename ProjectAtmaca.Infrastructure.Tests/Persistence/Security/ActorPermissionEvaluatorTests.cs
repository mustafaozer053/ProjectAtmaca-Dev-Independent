using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence;using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Security;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ActorPermissionEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_Should_GrantExactActorPermission()
    {
        ActorId actorId =
            ActorId.New();

        Permission permission =
            Permission.Create(
                $"Security.Granted.{Guid.NewGuid():N}");

        await PersistGrantAsync(
            actorId,
            permission);

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorPermissionEvaluator evaluator =
            new(context);

        using CancellationTokenSource cancellationSource =
            new();

        PermissionDecision decision =
            await evaluator.EvaluateAsync(
                actorId,
                permission,
                cancellationSource.Token);

        decision.Should()
            .Be(PermissionDecision.Granted);
    }

    [Fact]
    public async Task EvaluateAsync_Should_DenyWhenGrantDoesNotExist()
    {
        ActorId actorId =
            ActorId.New();

        Permission permission =
            Permission.Create(
                $"Security.Missing.{Guid.NewGuid():N}");

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorPermissionEvaluator evaluator =
            new(context);

        using CancellationTokenSource cancellationSource =
            new();

        PermissionDecision decision =
            await evaluator.EvaluateAsync(
                actorId,
                permission,
                cancellationSource.Token);

        decision.Should()
            .Be(PermissionDecision.Denied);
    }

    [Fact]
    public async Task EvaluateAsync_Should_NotUseGrantBelongingToDifferentActor()
    {
        ActorId grantedActorId =
            ActorId.New();

        ActorId requestingActorId =
            ActorId.New();

        Permission permission =
            Permission.Create(
                $"Security.ActorIsolation.{Guid.NewGuid():N}");

        await PersistGrantAsync(
            grantedActorId,
            permission);

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorPermissionEvaluator evaluator =
            new(context);

        using CancellationTokenSource cancellationSource =
            new();

        PermissionDecision decision =
            await evaluator.EvaluateAsync(
                requestingActorId,
                permission,
                cancellationSource.Token);

        decision.Should()
            .Be(PermissionDecision.Denied);
    }

    [Fact]
    public async Task EvaluateAsync_Should_RequireExactPermissionCodeCase()
    {
        ActorId actorId =
            ActorId.New();

        string discriminator =
            Guid.NewGuid()
                .ToString("N");

        Permission grantedPermission =
            Permission.Create(
                $"Security.ExactCase.{discriminator}");

        Permission differentlyCasedPermission =
            Permission.Create(
                $"security.exactcase.{discriminator}");

        await PersistGrantAsync(
            actorId,
            grantedPermission);

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorPermissionEvaluator evaluator =
            new(context);

        using CancellationTokenSource cancellationSource =
            new();

        PermissionDecision decision =
            await evaluator.EvaluateAsync(
                actorId,
                differentlyCasedPermission,
                cancellationSource.Token);

        decision.Should()
            .Be(PermissionDecision.Denied);
    }

    [Fact]
    public async Task EvaluateAsync_Should_DenyDefaultActorId()
    {
        Permission permission =
            Permission.Create(
                $"Security.DefaultActor.{Guid.NewGuid():N}");

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorPermissionEvaluator evaluator =
            new(context);

        using CancellationTokenSource cancellationSource =
            new();

        PermissionDecision decision =
            await evaluator.EvaluateAsync(
                default,
                permission,
                cancellationSource.Token);

        decision.Should()
            .Be(PermissionDecision.Denied);
    }

    [Fact]
    public async Task EvaluateAsync_Should_RejectNullPermission()
    {
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorPermissionEvaluator evaluator =
            new(context);

        using CancellationTokenSource cancellationSource =
            new();

        Func<Task> evaluate =
            async () =>
            {
                await evaluator.EvaluateAsync(
                    ActorId.New(),
                    null!,
                    cancellationSource.Token);
            };

        await evaluate.Should()
            .ThrowAsync<ArgumentNullException>();
    }

    private static async Task PersistGrantAsync(
        ActorId actorId,
        Permission permission)
    {
        ActorPermissionGrant grant =
            ActorPermissionGrant.Create(
                actorId,
                permission);

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        context
            .Set<ActorPermissionGrant>()
            .Add(grant);

        await context.SaveChangesAsync();
    }
}