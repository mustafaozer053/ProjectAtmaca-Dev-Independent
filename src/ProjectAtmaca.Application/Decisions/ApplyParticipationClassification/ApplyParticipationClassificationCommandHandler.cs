using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;

public sealed class ApplyParticipationClassificationCommandHandler
{
    private readonly IDecisionRepository
        _decisionRepository;

    private readonly IParticipationRepository
        _participationRepository;

    private readonly IDecisionApplicationRepository
        _decisionApplicationRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public ApplyParticipationClassificationCommandHandler(
        IDecisionRepository decisionRepository,
        IParticipationRepository participationRepository,
        IDecisionApplicationRepository decisionApplicationRepository,
        IUnitOfWork unitOfWork)
    {
        _decisionRepository =
            decisionRepository;

        _participationRepository =
            participationRepository;

        _decisionApplicationRepository =
            decisionApplicationRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result> Handle(
        ApplyParticipationClassificationCommand command,
        CancellationToken cancellationToken = default)
    {
        Decision? decision =
            await _decisionRepository.GetByIdAsync(
                command.DecisionId,
                cancellationToken);

        if (decision is null)
        {
            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .DecisionNotFound);
        }

        if (decision.Revision != command.DecisionRevision)
        {
            return Result.Failure(
                ApplyParticipationClassificationErrors
                    .RevisionMismatch);
        }

        if (decision.SupersededByDecisionId is not null)
        {
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

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result.Success();
    }
}
