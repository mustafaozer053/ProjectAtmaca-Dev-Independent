using Microsoft.Extensions.Logging;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

public sealed class ApplyParticipationClassificationCommandHandler
{
    private readonly ILogger<
        ApplyParticipationClassificationCommandHandler> _logger;

    private readonly IDecisionApplicationMetrics
        _decisionApplicationMetrics;

    private readonly IDecisionRepository
        _decisionRepository;

    private readonly IParticipationRepository
        _participationRepository;

    private readonly IDecisionApplicationRepository
        _decisionApplicationRepository;

    private readonly IDecisionAuthorityCommitter
        _decisionAuthorityCommitter;

    private readonly IDecisionApplicationOperationStore
    _decisionApplicationOperationStore;

    public ApplyParticipationClassificationCommandHandler(
        IDecisionApplicationMetrics decisionApplicationMetrics,
        IDecisionApplicationOperationStore decisionApplicationOperationStore,
        IDecisionRepository decisionRepository,
        IParticipationRepository participationRepository,
        IDecisionApplicationRepository decisionApplicationRepository,
        IDecisionAuthorityCommitter decisionAuthorityCommitter,
        ILogger<ApplyParticipationClassificationCommandHandler> logger)
    {
        _decisionApplicationMetrics =
            decisionApplicationMetrics
            ?? throw new ArgumentNullException(
                nameof(decisionApplicationMetrics));

        _decisionApplicationOperationStore =
            decisionApplicationOperationStore;

        _decisionRepository =
            decisionRepository;

        _participationRepository =
            participationRepository;

        _decisionApplicationRepository =
            decisionApplicationRepository;

        _decisionAuthorityCommitter =
            decisionAuthorityCommitter;

        _logger =
            logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    public async Task<Result> Handle(
        ApplyParticipationClassificationCommand command,
        CancellationToken cancellationToken = default)
    {
        DecisionApplicationOperation? completedOperation =
            await _decisionApplicationOperationStore.GetByIdAsync(
                command.OperationId,
                cancellationToken);

        if (completedOperation is not null)
        {
            bool isExactReplay =
                completedOperation.DecisionId ==
                    command.DecisionId
                &&
                completedOperation.DecisionRevision ==
                    command.DecisionRevision
                &&
                completedOperation.AppliedAtUtc ==
                    command.AppliedAtUtc;

            if (isExactReplay)
            {
                _logger.LogInformation(
                    new EventId(
                        id: 0,
                        name: "DecisionApplicationReplayed"),
                    "Decision application operation {OperationId} "
                    + "for decision {DecisionId} at revision "
                    + "{DecisionRevision} completed with outcome "
                    + "{Outcome}.",
                    completedOperation.OperationId.Value,
                    completedOperation.DecisionId.Value,
                    completedOperation.DecisionRevision.Value,
                    "Replay");

                _decisionApplicationMetrics
                    .RecordReplayOutcome();

                return Result.Success();
            }

            ObserveRejectedOutcome(
                command,
                ApplyParticipationClassificationErrors
                    .OperationConflict);

            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .OperationConflict);
        }

        Decision? decision =
            await _decisionRepository.GetByIdAsync(
                command.DecisionId,
                cancellationToken);

        if (decision is null)
        {
            ObserveRejectedOutcome(
                command,
                ApplyParticipationClassificationErrors
                    .DecisionNotFound);

            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .DecisionNotFound);
        }

        if (decision.Revision != command.DecisionRevision)
        {
            ObserveRejectedOutcome(
                command,
                ApplyParticipationClassificationErrors
                    .RevisionMismatch);

            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .RevisionMismatch);
        }

        if (decision.SupersededByDecisionId is not null)
        {
            ObserveRejectedOutcome(
                command,
                ApplyParticipationClassificationErrors
                    .DecisionSuperseded);

            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .DecisionSuperseded);
        }

        ParticipationId participationId =
            ParticipationId.From(
                decision.Target.TargetId);

        Participation? participation =
                await _participationRepository.GetByIdAsync(
                    participationId,
                    cancellationToken);

        if (participation is null)
        {
            ObserveRejectedOutcome(
                command,
                ApplyParticipationClassificationErrors
                    .ParticipationNotFound);

            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .ParticipationNotFound);
        }

        if (decision.Effect.Outcome ==
            ParticipationClassificationOutcome.Present)
        {
            participation.MarkPresent();
        }

        if (decision.Effect.Outcome ==
            ParticipationClassificationOutcome.Absent)
        {
            participation.MarkAbsent();
        }

        DecisionApplication decisionApplication =
            DecisionApplication.Create(
                decision.DecisionId,
                decision.Target,
                decision.Revision,
                command.AppliedAtUtc);

        await _decisionApplicationRepository.AddAsync(
            decisionApplication,
            cancellationToken);

        var operation =
            new DecisionApplicationOperation(
                command.OperationId,
                decision.DecisionId,
                decision.Revision,
                command.AppliedAtUtc);

        await _decisionApplicationOperationStore.AddAsync(
            operation,
            cancellationToken);

        DecisionAuthorityCommitOutcome commitOutcome =
            await _decisionAuthorityCommitter.CommitAsync(
                command.OperationId,
                decision.DecisionId,
                decision.Revision,
                cancellationToken);

        if (commitOutcome ==
            DecisionAuthorityCommitOutcome.AuthorityLost)
        {
            ObserveRejectedOutcome(
                command,
                ApplyParticipationClassificationErrors
                    .DecisionAuthorityLost);

            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .DecisionAuthorityLost);
        }

        if (commitOutcome ==
            DecisionAuthorityCommitOutcome.OperationAlreadyExists)
        {
            DecisionApplicationOperation? durableOperation =
                await _decisionApplicationOperationStore.GetByIdAsync(
                    command.OperationId,
                    cancellationToken);

            bool isExactReplay =
                durableOperation is not null
                &&
                durableOperation.DecisionId ==
                    command.DecisionId
                &&
                durableOperation.DecisionRevision ==
                    command.DecisionRevision
                &&
                durableOperation.AppliedAtUtc ==
                    command.AppliedAtUtc;

            if (isExactReplay)
            {
                _logger.LogInformation(
                    new EventId(
                        id: 0,
                        name: "DecisionApplicationReplayed"),
                    "Decision application operation {OperationId} "
                    + "for decision {DecisionId} at revision "
                    + "{DecisionRevision} completed with outcome "
                    + "{Outcome}.",
                    command.OperationId.Value,
                    command.DecisionId.Value,
                    command.DecisionRevision.Value,
                    "Replay");

                _decisionApplicationMetrics
                    .RecordReplayOutcome();

                return Result.Success();
            }

            ObserveRejectedOutcome(
                command,
                ApplyParticipationClassificationErrors
                    .OperationConflict);

            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .OperationConflict);
        }

        _logger.LogInformation(
            new EventId(
                id: 0,
                name: "DecisionApplicationApplied"),
            "Decision application operation {OperationId} "
            + "for decision {DecisionId} at revision "
            + "{DecisionRevision} completed with outcome "
            + "{Outcome}.",
            command.OperationId.Value,
            decision.DecisionId.Value,
            decision.Revision.Value,
            "Applied");

        _decisionApplicationMetrics
            .RecordAppliedOutcome();

        return Result.Success();
    }

    private void ObserveRejectedOutcome(
        ApplyParticipationClassificationCommand command,
        Error error)
    {
        _logger.LogInformation(
            new EventId(
                id: 0,
                name: "DecisionApplicationRejected"),
            "Decision application operation {OperationId} "
            + "for decision {DecisionId} at revision "
            + "{DecisionRevision} completed with outcome "
            + "{Outcome} and error code {ErrorCode}.",
            command.OperationId.Value,
            command.DecisionId.Value,
            command.DecisionRevision.Value,
            "Rejected",
            error.Code);

        _decisionApplicationMetrics
            .RecordRejectedOutcome(
                error.Code);
    }
}
