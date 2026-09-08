using ProjectAtmaca.Application
    .Abstractions.Persistence;
using ProjectAtmaca.Application
    .Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.RecordDeparture;

public sealed class RecordParticipationDepartureCommandHandler
{
    private readonly IActorAuthorizationService
        _authorizationService;

    private readonly IParticipationRepository
        _participationRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public RecordParticipationDepartureCommandHandler(
        IActorAuthorizationService authorizationService,
        IParticipationRepository participationRepository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService =
            authorizationService;

        _participationRepository =
            participationRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result> Handle(
        RecordParticipationDepartureCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Participations.RecordDeparture,
                cancellationToken);

        if (authorizationResult.IsFailure)
        {
            return authorizationResult;
        }

        Participation? participation =
            await _participationRepository.GetByIdAsync(
                command.ParticipationId,
                cancellationToken);

        if (participation is null)
        {
            return Result.Failure(
                RecordParticipationDepartureErrors
                    .NotFound);
        }

        Result recordDepartureResult =
            participation.RecordDeparture(
                command.LeftAt);

        if (recordDepartureResult.IsFailure)
        {
            return recordDepartureResult;
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result.Success();
    }
}
