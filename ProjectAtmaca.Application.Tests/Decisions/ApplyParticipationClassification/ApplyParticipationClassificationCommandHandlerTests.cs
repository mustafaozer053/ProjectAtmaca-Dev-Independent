using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests.Decisions
    .ApplyParticipationClassification;

public sealed class
    ApplyParticipationClassificationCommandHandlerTests
{

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithoutReapplying_WhenCompletedOperationIsReplayed()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                31,
                10,
                0,
                0,
                TimeSpan.Zero);

        var operationRepository =
            new FakeDecisionApplicationOperationStore()
            {
                OperationToReturn =
                    new DecisionApplicationOperation(
                        operationId,
                        decision.DecisionId,
                        decision.Revision,
                        appliedAtUtc)
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationRepository,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        operationRepository.GetByIdCallCount
            .Should()
            .Be(1);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task
    Handle_Should_ReturnConflict_WhenOperationIdIsReusedWithDifferentAppliedAtUtc()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset completedAppliedAtUtc =
            new(
                2026,
                8,
                31,
                10,
                0,
                0,
                TimeSpan.Zero);

        DateTimeOffset replayedAppliedAtUtc =
            completedAppliedAtUtc.AddMinutes(1);

        var operationStore =
            new FakeDecisionApplicationOperationStore()
            {
                OperationToReturn =
                    new DecisionApplicationOperation(
                        operationId,
                        decision.DecisionId,
                        decision.Revision,
                        completedAppliedAtUtc)
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                replayedAppliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .OperationConflict);

        operationStore.GetByIdCallCount
            .Should()
            .Be(1);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task
    Handle_Should_ReturnConflict_WhenOperationIdIsReusedWithDifferentDecisionId()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                31,
                10,
                0,
                0,
                TimeSpan.Zero);

        var operationStore =
            new FakeDecisionApplicationOperationStore
            {
                OperationToReturn =
                    new DecisionApplicationOperation(
                        operationId,
                        decision.DecisionId,
                        decision.Revision,
                        appliedAtUtc)
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                DecisionId.New(),
                decision.Revision,
                appliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .OperationConflict);

        operationStore.GetByIdCallCount
            .Should()
            .Be(1);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task
    Handle_Should_ReturnConflict_WhenOperationIdIsReusedWithDifferentDecisionRevision()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                31,
                10,
                0,
                0,
                TimeSpan.Zero);

        var operationStore =
            new FakeDecisionApplicationOperationStore
            {
                OperationToReturn =
                    new DecisionApplicationOperation(
                        operationId,
                        decision.DecisionId,
                        decision.Revision,
                        appliedAtUtc)
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        DecisionRevision differentRevision =
            decision.Revision.Next();

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                differentRevision,
                appliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .OperationConflict);

        operationStore.GetByIdCallCount
            .Should()
            .Be(1);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(0);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Handle_Should_FailWithoutMutationOrCommit_WhenRequestedRevisionIsNotCurrent()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        decision.ReEvaluate(
            ParticipationClassificationSnapshot.Create(
                participation.ActivityReference,
                participation.AtmacaCardId,
                ParticipationStatus.Present,
                null,
                null,
                null),
            ParticipationClassificationEffect.Absent());

        DecisionRevision requestedRevision =
            DecisionRevision.Initial;

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn = decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                requestedRevision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    7,
                    0,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .RevisionMismatch);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(0);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenDecisionAuthorityIsLostBeforeCommit()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter
            {
                OutcomeToReturn =
                    DecisionAuthorityCommitOutcome.AuthorityLost
            };

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    29,
                    11,
                    0,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .DecisionAuthorityLost);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);

        authorityCommitter.DecisionIdReceived
            .Should()
            .Be(
                decision.DecisionId);

        authorityCommitter.ExpectedRevisionReceived
            .Should()
            .Be(
                decision.Revision);
    }

    [Fact]
    public async Task Handle_Should_FailWithoutMutationOrCommit_WhenDecisionDoesNotExist()
    {
        // Arrange
        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn = null
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn = null
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                DecisionId.New(),
                DecisionRevision.Initial,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    7,
                    30,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .DecisionNotFound);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(0);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Handle_Should_LoadParticipationFromDecisionTarget_WhenRevisionIsCurrent()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn = decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    8,
                    0,
                    0,
                    TimeSpan.Zero));

        // Act
        await handler.Handle(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        participationRepository.GetByIdCallCount
            .Should()
            .Be(1);

        participationRepository.RequestedParticipationId
            .Should()
            .Be(
                ParticipationId.From(
                    decision.Target.TargetId));

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Handle_Should_FailWithoutProvenanceOrCommit_WhenTargetParticipationDoesNotExist()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    null
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    8,
                    30,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .ParticipationNotFound);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(1);

        participationRepository.RequestedParticipationId
            .Should()
            .Be(
                ParticipationId.From(
                    decision.Target.TargetId));

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Handle_Should_MarkParticipationPresent_WhenDecisionEffectIsPresent()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        participation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        Decision decision =
            CreateDecision(
                participation);

        decision.Effect.Outcome
            .Should()
            .Be(
                ParticipationClassificationOutcome
                    .Present);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    9,
                    0,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        participation.Condition
            .Should()
            .BeNull();

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Handle_Should_MarkParticipationAbsent_WhenDecisionEffectIsAbsent()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                participation.ActivityReference,
                participation.AtmacaCardId,
                participation.Status,
                participation.Condition,
                participation.JoinedAt,
                participation.LeftAt);

        Decision decision =
            Decision.CreateParticipationClassification(
                participation.ParticipationId,
                snapshot,
                ParticipationClassificationEffect.Absent());

        decision.Effect.Outcome
            .Should()
            .Be(
                ParticipationClassificationOutcome.Absent);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    9,
                    30,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Absent);

        participation.Condition
            .Should()
            .BeNull();

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Handle_Should_FailWithoutMutationOrCommit_WhenDecisionIsSuperseded()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        decision.SupersedeBy(
            DecisionId.New());

        decision.SupersededByDecisionId
            .Should()
            .NotBeNull();

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    10,
                    0,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .DecisionSuperseded);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(0);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(0);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Handle_Should_CreateExactDecisionApplicationProvenance_WhenEffectIsApplied()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                28,
                10,
                30,
                0,
                TimeSpan.Zero);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        DecisionApplication decisionApplication =
            decisionApplicationRepository
                .AddedDecisionApplication!;

        decisionApplication.DecisionId
            .Should()
            .Be(
                decision.DecisionId);

        decisionApplication.Target
            .Should()
            .Be(
                decision.Target);

        decisionApplication.AppliedDecisionRevision
            .Should()
            .Be(
                decision.Revision);

        decisionApplication.AppliedAtUtc
            .Should()
            .Be(
                appliedAtUtc);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Handle_Should_CommitExactlyOnce_WhenDecisionApplicationSucceeds()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    11,
                    0,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Handle_Should_CommitThroughExactDecisionAuthority_WhenDecisionApplicationSucceeds()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    29,
                    10,
                    30,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);

        authorityCommitter.DecisionIdReceived
            .Should()
            .Be(
                decision.DecisionId);

        authorityCommitter.ExpectedRevisionReceived
            .Should()
            .Be(
                decision.Revision);
    }

    [Fact]
    public async Task
Handle_Should_CommitThroughExactOperationIdentity_WhenDecisionApplicationSucceeds()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    9,
                    1,
                    13,
                    30,
                    0,
                    TimeSpan.Zero));

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);

        authorityCommitter.OperationIdReceived
            .Should()
            .Be(operationId);

        authorityCommitter.DecisionIdReceived
            .Should()
            .Be(decision.DecisionId);

        authorityCommitter.ExpectedRevisionReceived
            .Should()
            .Be(decision.Revision);
    }

    [Fact]
    public async Task Handle_Should_PropagateCancellationToken_ThroughEntireApplicationFlow()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    11,
                    30,
                    0,
                    TimeSpan.Zero));

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        // Act
        var result =
            await handler.Handle(
                command,
                cancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        decisionRepository.CancellationTokenReceived
            .Should()
            .Be(cancellationToken);

        participationRepository.CancellationTokenReceived
            .Should()
            .Be(cancellationToken);

        decisionApplicationRepository.CancellationTokenReceived
            .Should()
            .Be(cancellationToken);

        authorityCommitter.CancellationTokenReceived
            .Should()
            .Be(cancellationToken);
    }

    [Fact]
    public async Task Handle_Should_PropagatePersistenceException_WhenCommitFails()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var persistenceException =
            new InvalidOperationException(
                "Simulated persistence failure.");

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter
            {
                ExceptionToThrow =
                    persistenceException
            };

        var operationStore =
            new FakeDecisionApplicationOperationStore();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                DecisionApplicationOperationId.New(),
                decision.DecisionId,
                decision.Revision,
                new DateTimeOffset(
                    2026,
                    8,
                    28,
                    12,
                    0,
                    0,
                    TimeSpan.Zero));

        // Act
        Func<Task> act =
            async () =>
                await handler.Handle(
                    command,
                    TestContext.Current.CancellationToken);

        // Assert
        await act
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .Where(
                exception =>
                    ReferenceEquals(
                        exception,
                        persistenceException));

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
    Handle_Should_ApplyAgain_WhenSameDecisionRevisionIsUsedWithDifferentOperationId()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId previousOperationId =
            DecisionApplicationOperationId.New();

        DecisionApplicationOperationId newOperationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset previousAppliedAtUtc =
            new(
                2026,
                8,
                31,
                10,
                0,
                0,
                TimeSpan.Zero);

        DateTimeOffset newAppliedAtUtc =
            previousAppliedAtUtc.AddMinutes(30);

        var operationStore =
            new FakeDecisionApplicationOperationStore
            {
                OperationToReturn = null
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                newOperationId,
                decision.DecisionId,
                decision.Revision,
                newAppliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        newOperationId
            .Should()
            .NotBe(previousOperationId);

        operationStore.GetByIdCallCount
            .Should()
            .Be(1);

        operationStore.OperationIdReceived
            .Should()
            .Be(newOperationId);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(1);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(1);

        participation.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        decisionApplicationRepository
            .AddedDecisionApplication
            .Should()
            .NotBeNull();

        decisionApplicationRepository
            .AddedDecisionApplication!
            .DecisionId
            .Should()
            .Be(
                decision.DecisionId);

        decisionApplicationRepository
            .AddedDecisionApplication!
            .AppliedDecisionRevision
            .Should()
            .Be(
                decision.Revision);

        decisionApplicationRepository
            .AddedDecisionApplication!
            .AppliedAtUtc
            .Should()
            .Be(
                newAppliedAtUtc);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);

        authorityCommitter.DecisionIdReceived
            .Should()
            .Be(
                decision.DecisionId);

        authorityCommitter.ExpectedRevisionReceived
            .Should()
            .Be(
                decision.Revision);
    }

    [Fact]
    public async Task
    Handle_Should_RecordOperation_WhenNewApplicationSucceeds()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                31,
                11,
                0,
                0,
                TimeSpan.Zero);

        var operationStore =
            new FakeDecisionApplicationOperationStore
            {
                OperationToReturn = null
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        operationStore.AddCallCount
            .Should()
            .Be(1);

        operationStore.AddedOperation
            .Should()
            .NotBeNull();

        operationStore.AddedOperation!
            .OperationId
            .Should()
            .Be(
                operationId);

        operationStore.AddedOperation!
            .DecisionId
            .Should()
            .Be(
                decision.DecisionId);

        operationStore.AddedOperation!
            .DecisionRevision
            .Should()
            .Be(
                decision.Revision);

        operationStore.AddedOperation!
            .AppliedAtUtc
            .Should()
            .Be(
                appliedAtUtc);
    }

    [Fact]
    public async Task
        Handle_Should_ReturnSuccess_WhenConcurrentOperationWasAlreadyCompletedWithSameSemantics()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                11,
                30,
                0,
                TimeSpan.Zero);

        DecisionApplicationOperation completedOperation =
            new(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        var operationStore =
            new FakeDecisionApplicationOperationStore
            {
                OperationsToReturn =
                [
                    null,
                completedOperation
                ]
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter
            {
                OutcomeToReturn =
                    DecisionAuthorityCommitOutcome
                        .OperationAlreadyExists
            };

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        operationStore.GetByIdCallCount
            .Should()
            .Be(2);

        operationStore.OperationIdReceived
            .Should()
            .Be(operationId);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(1);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(1);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        operationStore.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        Handle_Should_ReturnConflict_WhenConcurrentOperationWasCompletedWithDifferentSemantics()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset requestedAppliedAtUtc =
            new(
                2026,
                9,
                1,
                12,
                0,
                0,
                TimeSpan.Zero);

        DateTimeOffset durableAppliedAtUtc =
            requestedAppliedAtUtc.AddMinutes(-1);

        DecisionApplicationOperation durableOperation =
            new(
                operationId,
                decision.DecisionId,
                decision.Revision,
                durableAppliedAtUtc);

        var operationStore =
            new FakeDecisionApplicationOperationStore
            {
                OperationsToReturn =
                [
                    null,
                durableOperation
                ]
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter
            {
                OutcomeToReturn =
                    DecisionAuthorityCommitOutcome
                        .OperationAlreadyExists
            };

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                requestedAppliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .OperationConflict);

        operationStore.GetByIdCallCount
            .Should()
            .Be(2);

        operationStore.OperationIdReceived
            .Should()
            .Be(operationId);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(1);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(1);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        operationStore.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        Handle_Should_ReturnConflict_WhenOperationAlreadyExistsButDurableOperationCannotBeLoaded()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        Decision decision =
            CreateDecision(
                participation);

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                12,
                30,
                0,
                TimeSpan.Zero);

        var operationStore =
            new FakeDecisionApplicationOperationStore
            {
                OperationsToReturn =
                [
                    null,
                null
                ]
            };

        var decisionRepository =
            new FakeDecisionRepository
            {
                DecisionToReturn =
                    decision
            };

        var participationRepository =
            new FakeParticipationRepository
            {
                ParticipationToReturn =
                    participation
            };

        var decisionApplicationRepository =
            new FakeDecisionApplicationRepository();

        var authorityCommitter =
            new FakeDecisionAuthorityCommitter
            {
                OutcomeToReturn =
                    DecisionAuthorityCommitOutcome
                        .OperationAlreadyExists
            };

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                operationStore,
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                authorityCommitter);

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        // Act
        var result =
            await handler.Handle(
                command,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .OperationConflict);

        operationStore.GetByIdCallCount
            .Should()
            .Be(2);

        operationStore.OperationIdReceived
            .Should()
            .Be(operationId);

        decisionRepository.GetByIdCallCount
            .Should()
            .Be(1);

        participationRepository.GetByIdCallCount
            .Should()
            .Be(1);

        decisionApplicationRepository.AddCallCount
            .Should()
            .Be(1);

        operationStore.AddCallCount
            .Should()
            .Be(1);

        authorityCommitter.CommitCallCount
            .Should()
            .Be(1);
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

    private static Decision CreateDecision(
        Participation participation)
    {
        var snapshot =
            ParticipationClassificationSnapshot.Create(
                participation.ActivityReference,
                participation.AtmacaCardId,
                participation.Status,
                participation.Condition,
                participation.JoinedAt,
                participation.LeftAt);

        return Decision.CreateParticipationClassification(
            participation.ParticipationId,
            snapshot,
            ParticipationClassificationEffect.Present());
    }

    private sealed class FakeDecisionRepository
    : IDecisionRepository
    {
        public Decision? DecisionToReturn { get; init; }

        public int GetByIdCallCount { get; private set; }

        public Task<Decision?> GetByIdAsync(
            DecisionId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            CancellationTokenReceived =
                cancellationToken;

            return Task.FromResult(
                DecisionToReturn);
        }

        public CancellationToken CancellationTokenReceived
        {
            get;
            private set;
        }
    }

    private sealed class FakeParticipationRepository
        : IParticipationRepository
    {
        public Participation? ParticipationToReturn { get; init; }

        public int GetByIdCallCount { get; private set; }

        public ParticipationId? RequestedParticipationId
        {
            get;
            private set;
        }

        public Task<Participation?> GetByIdAsync(
            ParticipationId id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            RequestedParticipationId =
                id;

            CancellationTokenReceived =
                cancellationToken;

            return Task.FromResult(
                ParticipationToReturn);
        }

        public Task<bool> ExistsAsync(
            AtmacaCardId atmacaCardId,
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            Participation participation,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
        public CancellationToken CancellationTokenReceived
        {
            get;
            private set;
        }
    }

    private sealed class FakeDecisionApplicationRepository
        : IDecisionApplicationRepository
    {
        public int AddCallCount { get; private set; }

        public DecisionApplication? AddedDecisionApplication
        {
            get;
            private set;
        }

        public Task AddAsync(
            DecisionApplication decisionApplication,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;

            AddedDecisionApplication =
                decisionApplication;

            CancellationTokenReceived =
                cancellationToken;

            return Task.CompletedTask;
        }

        public CancellationToken CancellationTokenReceived
        {
            get;
            private set;
        }
    }

    private sealed class FakeDecisionAuthorityCommitter
        : IDecisionAuthorityCommitter
    {
        public int CommitCallCount { get; private set; }

        public DecisionId? DecisionIdReceived
        {
            get;
            private set;
        }

        public DecisionRevision? ExpectedRevisionReceived
        {
            get;
            private set;
        }

        public CancellationToken CancellationTokenReceived
        {
            get;
            private set;
        }

        public DecisionAuthorityCommitOutcome OutcomeToReturn
        {
            get;
            init;
        } =
            DecisionAuthorityCommitOutcome.Committed;

        public Exception? ExceptionToThrow
        {
            get;
            init;
        }

        public Task<DecisionAuthorityCommitOutcome> CommitAsync(
            DecisionApplicationOperationId operationId,
            DecisionId decisionId,
            DecisionRevision expectedRevision,
            CancellationToken cancellationToken = default)
        {
            CommitCallCount++;

            OperationIdReceived =
                operationId;

            DecisionIdReceived =
                decisionId;

            ExpectedRevisionReceived =
                expectedRevision;

            CancellationTokenReceived =
                cancellationToken;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(
                OutcomeToReturn);
        }

        public DecisionApplicationOperationId?
            OperationIdReceived
        {
            get;
            private set;
        }
    }

    private sealed class FakeDecisionApplicationOperationStore
    : IDecisionApplicationOperationStore
    {
        public DecisionApplicationOperation? OperationToReturn
        {
            get;
            init;
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public DecisionApplicationOperationId? OperationIdReceived
        {
            get;
            private set;
        }

        public CancellationToken CancellationTokenReceived
        {
            get;
            private set;
        }

        public Task<DecisionApplicationOperation?> GetByIdAsync(
            DecisionApplicationOperationId operationId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            OperationIdReceived =
                operationId;

            CancellationTokenReceived =
                cancellationToken;

            if (OperationsToReturn is not null)
            {
                int index =
                    GetByIdCallCount - 1;

                return Task.FromResult(
                    index < OperationsToReturn.Count
                        ? OperationsToReturn[index]
                        : OperationsToReturn[^1]);
            }

            return Task.FromResult(
                OperationToReturn);
        }

        public int AddCallCount { get; private set; }

        public DecisionApplicationOperation? AddedOperation
        {
            get;
            private set;
        }
        public Task AddAsync(
            DecisionApplicationOperation operation,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;

            AddedOperation =
                operation;

            return Task.CompletedTask;
        }

        public IReadOnlyList<DecisionApplicationOperation?>?
            OperationsToReturn
        {
            get;
            init;
        }
    }
}
