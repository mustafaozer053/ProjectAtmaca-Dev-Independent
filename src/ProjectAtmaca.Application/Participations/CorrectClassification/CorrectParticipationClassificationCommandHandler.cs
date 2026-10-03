using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Participations.CorrectClassification;

public sealed class CorrectParticipationClassificationCommandHandler(
    IActorAuthorizationService authorizationService,
    IParticipationRepository participationRepository,
    BtaEligibilityValidator btaEligibilityValidator,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        CorrectParticipationClassificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var permission = command.Status switch
        {
            ParticipationStatus.Present => Permissions.Participations.MarkPresent,
            ParticipationStatus.Absent => Permissions.Participations.MarkAbsent,
            _ => null
        };

        if (permission is null)
            return Result.Failure(ParticipationErrors.InvalidStatus);
        if (command.IsBta && command.Status != ParticipationStatus.Absent)
            return Result.Failure(ParticipationErrors.InvalidConditionForStatus);

        var authorization = await authorizationService.AuthorizeAsync(
            permission, cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        var participation = await participationRepository.GetByIdAsync(
            command.ParticipationId, cancellationToken);
        if (participation is null)
            return Result.Failure(CorrectParticipationClassificationErrors.NotFound);

        if (command.IsBta)
        {
            var eligibility = await btaEligibilityValidator.ValidateAsync(
                participation, cancellationToken);
            if (eligibility.IsFailure)
                return eligibility;
        }

        var corrected = participation.CorrectClassification(
            command.Status,
            command.IsBta ? ParticipationCondition.Bta : null);
        if (corrected.IsFailure)
            return corrected;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
