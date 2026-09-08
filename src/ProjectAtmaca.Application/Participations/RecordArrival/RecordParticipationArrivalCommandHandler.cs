using ProjectAtmaca.Application
    .Abstractions.Persistence;
using ProjectAtmaca.Application
    .Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.RecordArrival;

public sealed class RecordParticipationArrivalCommandHandler
{
    private readonly IActorAuthorizationService
        _authorizationService;

    private readonly IParticipationRepository
        _participationRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public RecordParticipationArrivalCommandHandler(
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
        RecordParticipationArrivalCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Participations.RecordArrival,
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
                RecordParticipationArrivalErrors.NotFound);
        }

        Result recordArrivalResult =
            participation.RecordArrival(
                command.JoinedAt);

        if (recordArrivalResult.IsFailure)
        {
            return recordArrivalResult;
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result.Success();
    }
}
