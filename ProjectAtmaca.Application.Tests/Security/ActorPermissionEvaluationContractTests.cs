using System.Reflection;

using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Application.Tests.Security;

public sealed class ActorPermissionEvaluationContractTests
{
    [Fact]
    public void DefaultDecision_Should_DenyAccess()
    {
        PermissionDecision decision =
            default;

        decision.Should().Be(
            PermissionDecision.Denied);
    }

    [Fact]
    public void Decision_Should_ExposeOnlyDeniedAndGrantedOutcomes()
    {
        PermissionDecision[] decisions =
            Enum.GetValues<PermissionDecision>();

        decisions.Should().Equal(
            PermissionDecision.Denied,
            PermissionDecision.Granted);
    }

    [Theory]
    [InlineData(PermissionDecision.Denied)]
    [InlineData(PermissionDecision.Granted)]
    public async Task Evaluator_Should_ReturnExplicitDecision_AndPreserveInputs(
        PermissionDecision configuredDecision)
    {
        ActorId actorId =
            ActorId.From(
                Guid.Parse(
                    "3ceeb99e-9818-437e-8436-b7ae873b2090"));

        Permission permission =
            Permissions.Participations.MarkPresent;

        using var cancellationSource =
            new CancellationTokenSource();

        var evaluator =
            new RecordingActorPermissionEvaluator(
                configuredDecision);

        PermissionDecision decision =
            await evaluator.EvaluateAsync(
                actorId,
                permission,
                cancellationSource.Token);

        decision.Should().Be(
            configuredDecision);

        evaluator.ObservedActorId
            .Should()
            .Be(actorId);

        evaluator.ObservedPermission
            .Should()
            .BeSameAs(permission);

        evaluator.ObservedCancellationToken
            .Should()
            .Be(cancellationSource.Token);
    }

    [Fact]
    public void EvaluatorContract_Should_RemainProviderNeutral()
    {
        MethodInfo[] methods =
            typeof(IActorPermissionEvaluator)
                .GetMethods();

        methods.Should().ContainSingle();

        MethodInfo method =
            methods.Single();

        method.Name.Should().Be(
            nameof(IActorPermissionEvaluator.EvaluateAsync));

        method.ReturnType.Should().Be(
            typeof(Task<PermissionDecision>));

        method
            .GetParameters()
            .Select(
                parameter =>
                    parameter.ParameterType)
            .Should()
            .Equal(
                typeof(ActorId),
                typeof(Permission),
                typeof(CancellationToken));
    }

    private sealed class RecordingActorPermissionEvaluator
        : IActorPermissionEvaluator
    {
        private readonly PermissionDecision
            _configuredDecision;

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
            PermissionDecision configuredDecision)
        {
            _configuredDecision =
                configuredDecision;
        }

        public Task<PermissionDecision> EvaluateAsync(
            ActorId actorId,
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            ObservedActorId =
                actorId;

            ObservedPermission =
                permission;

            ObservedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                _configuredDecision);
        }
    }
}