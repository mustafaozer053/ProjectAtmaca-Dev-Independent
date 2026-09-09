using System.Reflection;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Tests.Decisions
    .ApplyParticipationClassification;

public sealed class
    ApplyParticipationClassificationAuthorizationTests
{
    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutDecisionApplicationAccess_WhenActorLacksApplyPermission()
    {
        DenyingActorAuthorizationService
            authorizationService =
                new();

        IDecisionApplicationOperationStore operationStore =
            CreateUnexpectedCollaborator<
                IDecisionApplicationOperationStore>();

        IDecisionRepository decisionRepository =
            CreateUnexpectedCollaborator<
                IDecisionRepository>();

        IParticipationRepository participationRepository =
            CreateUnexpectedCollaborator<
                IParticipationRepository>();

        IDecisionApplicationRepository
            decisionApplicationRepository =
                CreateUnexpectedCollaborator<
                    IDecisionApplicationRepository>();

        IDecisionAuthorityCommitter authorityCommitter =
            CreateUnexpectedCollaborator<
                IDecisionAuthorityCommitter>();

        ApplyParticipationClassificationCommandHandler handler =
            new(
                authorizationService,
                NullDecisionApplicationMetrics.Instance,
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter,
                NullLogger<
                    ApplyParticipationClassificationCommandHandler>
                    .Instance);

        ApplyParticipationClassificationCommand command =
            new(
                DecisionApplicationOperationId.New(),
                DecisionId.New(),
                DecisionRevision.Initial,
                new DateTimeOffset(
                    2026,
                    9,
                    9,
                    7,
                    0,
                    0,
                    TimeSpan.Zero));

        using CancellationTokenSource cancellationSource =
            new();

        Result result =
            await handler.Handle(
                command,
                cancellationSource.Token);

        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .BeSameAs(
                ActorAuthorizationErrors.Forbidden);

        authorizationService.CallCount
            .Should()
            .Be(1);

        authorizationService.ObservedPermission
            .Should()
            .BeSameAs(
                Permissions.Decisions
                    .ApplyParticipationClassification);

        authorizationService.ObservedCancellationToken
            .Should()
            .Be(cancellationSource.Token);
    }

    private static T CreateUnexpectedCollaborator<T>()
        where T : class
    {
        return DispatchProxy.Create<
            T,
            UnexpectedCollaboratorProxy>();
    }

    private sealed class DenyingActorAuthorizationService
        : IActorAuthorizationService
    {
        public int CallCount { get; private set; }

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

        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ObservedPermission =
                permission;

            ObservedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                Result.Failure(
                    ActorAuthorizationErrors.Forbidden));
        }
    }

    private sealed class NullDecisionApplicationMetrics
        : IDecisionApplicationMetrics
    {
        public static NullDecisionApplicationMetrics Instance
        {
            get;
        } =
            new();

        private NullDecisionApplicationMetrics()
        {
        }

        public void RecordAppliedOutcome()
        {
        }

        public void RecordReplayOutcome()
        {
        }

        public void RecordRejectedOutcome(
            string reason)
        {
        }
    }

    public class UnexpectedCollaboratorProxy
        : DispatchProxy
    {
        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? arguments)
        {
            string methodName =
                targetMethod?.Name
                ?? "<unknown>";

            throw new InvalidOperationException(
                "Authorization did not precede collaborator " +
                $"access: {methodName}.");
        }
    }
}

internal sealed class GrantedActorAuthorizationService
    : IActorAuthorizationService
{
    public static GrantedActorAuthorizationService Instance
    {
        get;
    } =
        new();

    private GrantedActorAuthorizationService()
    {
    }

    public Task<Result> AuthorizeAsync(
        Permission permission,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            Result.Success());
    }
}
