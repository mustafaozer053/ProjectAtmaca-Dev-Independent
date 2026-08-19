using ProjectAtmaca.Application
    .Abstractions.Persistence;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.RecordArrival;

public sealed class RecordParticipationArrivalCommandHandler
{
    private readonly IParticipationRepository
        _participationRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public RecordParticipationArrivalCommandHandler(
        IParticipationRepository participationRepository,
        IUnitOfWork unitOfWork)
    {
        _participationRepository =
            participationRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result> Handle(
        RecordParticipationArrivalCommand command,
        CancellationToken cancellationToken = default)
    {
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
