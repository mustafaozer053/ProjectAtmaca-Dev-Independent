using ProjectAtmaca.Application
    .Abstractions.Persistence;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.RecordDeparture;

public sealed class RecordParticipationDepartureCommandHandler
{
    private readonly IParticipationRepository
        _participationRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public RecordParticipationDepartureCommandHandler(
        IParticipationRepository participationRepository,
        IUnitOfWork unitOfWork)
    {
        _participationRepository =
            participationRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result> Handle(
        RecordParticipationDepartureCommand command,
        CancellationToken cancellationToken = default)
    {
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
