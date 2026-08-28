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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(0);
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

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

        unitOfWork.SaveChangesCallCount
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
            .Should()
            .Be(1);
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

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.CancellationTokenReceived
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

        var unitOfWork =
            new FakeUnitOfWork
            {
                ExceptionToThrow =
                    persistenceException
            };

        var handler =
            new ApplyParticipationClassificationCommandHandler(
                decisionRepository,
                participationRepository,
                decisionApplicationRepository,
                unitOfWork);

        var command =
            new ApplyParticipationClassificationCommand(
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

        unitOfWork.SaveChangesCallCount
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

        public Task<Decision?> GetByIdAsync(
            DecisionId id,
            CancellationToken cancellationToken = default)
        {
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

    private sealed class FakeUnitOfWork
        : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            CancellationTokenReceived =
                cancellationToken;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(1);
        }

        public CancellationToken CancellationTokenReceived
        {
            get;
            private set;
        }
        public Exception? ExceptionToThrow
        {
            get;
            init;
        }
    }
}
