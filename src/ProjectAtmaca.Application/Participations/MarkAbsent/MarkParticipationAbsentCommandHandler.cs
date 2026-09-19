using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Participations.MarkAbsent;

public sealed class MarkParticipationAbsentCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly IParticipationRepository _participationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkParticipationAbsentCommandHandler(
        IActorAuthorizationService authorizationService,
        IParticipationRepository participationRepository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _participationRepository = participationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        MarkParticipationAbsentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.Participations.MarkAbsent,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        var participation = await _participationRepository.GetByIdAsync(
            command.ParticipationId,
            cancellationToken);
        if (participation is null)
            return Result.Failure(
                MarkParticipationAbsentErrors.NotFound);

        var result = participation.MarkAbsent(ParticipationCondition.Bta);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
