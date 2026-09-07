using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Tests.Security;

public sealed class ActorAuthorizationServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_Should_ReturnSuccess_WhenPermissionIsGranted()
    {
        ActorId actorId =
            ActorId.From(
                Guid.NewGuid());

        StubCurrentActor currentActor =
            new(
                actorId);

        RecordingActorPermissionEvaluator evaluator =
            new(
                PermissionDecision.Granted);

        IActorAuthorizationService service =
            new ActorAuthorizationService(
                currentActor,
                evaluator);

        using CancellationTokenSource cancellationSource =
            new();

        Result result =
            await service.AuthorizeAsync(
                Permissions.Participations.Create,
                cancellationSource.Token);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        evaluator.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_ReturnForbidden_WhenPermissionIsDenied()
    {
        ActorId actorId =
            ActorId.From(
                Guid.NewGuid());

        StubCurrentActor currentActor =
            new(
                actorId);

        RecordingActorPermissionEvaluator evaluator =
            new(
                PermissionDecision.Denied);

        IActorAuthorizationService service =
            new ActorAuthorizationService(
                currentActor,
                evaluator);

        using CancellationTokenSource cancellationSource =
            new();

        Result result =
            await service.AuthorizeAsync(
                Permissions.Participations.MarkPresent,
                cancellationSource.Token);

        result.IsFailure.Should().BeTrue();

        result.Error.Should().BeSameAs(
            ActorAuthorizationErrors.Forbidden);

        evaluator.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_EvaluateCurrentActorForRequestedPermission()
    {
        ActorId actorId =
            ActorId.From(
                Guid.NewGuid());

        Permission permission =
            Permissions.Decisions
                .ApplyParticipationClassification;

        StubCurrentActor currentActor =
            new(
                actorId);

        RecordingActorPermissionEvaluator evaluator =
            new(
                PermissionDecision.Granted);

        IActorAuthorizationService service =
            new ActorAuthorizationService(
                currentActor,
                evaluator);

        using CancellationTokenSource cancellationSource =
            new();

        await service.AuthorizeAsync(
            permission,
            cancellationSource.Token);

        evaluator.ObservedActorId
            .Should()
            .Be(actorId);

        evaluator.ObservedPermission
            .Should()
            .BeSameAs(permission);

        evaluator.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_ForwardCancellationToken()
    {
        StubCurrentActor currentActor =
            new(
                ActorId.From(
                    Guid.NewGuid()));

        RecordingActorPermissionEvaluator evaluator =
            new(
                PermissionDecision.Granted);

        IActorAuthorizationService service =
            new ActorAuthorizationService(
                currentActor,
                evaluator);

        using CancellationTokenSource cancellationSource =
            new();

        await service.AuthorizeAsync(
            Permissions.Participations
                .RecordDeparture,
            cancellationSource.Token);

        evaluator.ObservedCancellationToken
            .Should()
            .Be(cancellationSource.Token);
    }

    [Fact]
    public void ForbiddenError_Should_HaveStableContract()
    {
        ActorAuthorizationErrors.Forbidden.Code
            .Should()
            .Be(
                "Security.Authorization.Forbidden");

        ActorAuthorizationErrors.Forbidden.Message
            .Should()
            .Be(
                "The current actor is not authorized " +
                "to perform this operation.");
    }

    private sealed class StubCurrentActor
        : ICurrentActor
    {
        public ActorId ActorId { get; }

        public StubCurrentActor(
            ActorId actorId)
        {
            ActorId =
                actorId;
        }
    }

    private sealed class RecordingActorPermissionEvaluator
        : IActorPermissionEvaluator
    {
        private readonly PermissionDecision
            _decision;

        public int CallCount { get; private set; }

        public ActorId? ObservedActorId
        {
            get;
            private set;
        }

        public Permission? ObservedPermission
        {
            get;
            private set;
        }

        public CancellationToken
            ObservedCancellationToken
        {
            get;
            private set;
        }

        public RecordingActorPermissionEvaluator(
            PermissionDecision decision)
        {
            _decision =
                decision;
        }

        public Task<PermissionDecision> EvaluateAsync(
            ActorId actorId,
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ObservedActorId =
                actorId;

            ObservedPermission =
                permission;

            ObservedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                _decision);
        }
    }
}
