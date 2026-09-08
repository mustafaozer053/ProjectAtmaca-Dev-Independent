using ProjectAtmaca.Application
    .Abstractions.Persistence;
using ProjectAtmaca.Application
    .Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application
    .Participations.MarkPresent;

public sealed class MarkParticipationPresentCommandHandler
{
    private readonly IActorAuthorizationService
        _authorizationService;

    private readonly IParticipationRepository
        _participationRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public MarkParticipationPresentCommandHandler(
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
        MarkParticipationPresentCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Participations.MarkPresent,
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
                MarkParticipationPresentErrors.NotFound);
        }

        Result markPresentResult =
            participation.MarkPresent(
                command.Condition);

        if (markPresentResult.IsFailure)
        {
            return markPresentResult;
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result.Success();
    }
}
