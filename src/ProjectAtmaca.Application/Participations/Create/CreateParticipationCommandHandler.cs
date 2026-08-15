using ProjectAtmaca.Application
    .Abstractions.Persistence;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.Create;

public sealed class CreateParticipationCommandHandler
{
    private readonly IParticipationRepository
        _participationRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public CreateParticipationCommandHandler(
        IParticipationRepository participationRepository,
        IUnitOfWork unitOfWork)
    {
        _participationRepository =
            participationRepository;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Result<ParticipationId>> Handle(
        CreateParticipationCommand command,
        CancellationToken cancellationToken = default)
    {
        bool alreadyExists =
            await _participationRepository.ExistsAsync(
                command.AtmacaCardId,
                command.ActivityReference,
                cancellationToken);

        if (alreadyExists)
        {
            return Result<ParticipationId>.Failure(
                CreateParticipationErrors.AlreadyExists);
        }

        Result<Participation> creationResult =
            Participation.Create(
                command.ActivityReference,
                command.AtmacaCardId);

        if (creationResult.IsFailure)
        {
            return Result<ParticipationId>.Failure(
                creationResult.Error!);
        }

        Participation participation =
            creationResult.Value!;

        await _participationRepository.AddAsync(
            participation,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ParticipationId>.Success(
            participation.ParticipationId);
    }
}