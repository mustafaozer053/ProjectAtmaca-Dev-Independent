using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Application.Participations.MarkAbsent;

public sealed class MarkParticipationAbsentCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly IParticipationRepository _participationRepository;
    private readonly BtaEligibilityValidator _btaEligibilityValidator;
    private readonly IUnitOfWork _unitOfWork;

    public MarkParticipationAbsentCommandHandler(
        IActorAuthorizationService authorizationService,
        IParticipationRepository participationRepository,
        BtaEligibilityValidator btaEligibilityValidator,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _participationRepository = participationRepository;
        _btaEligibilityValidator = btaEligibilityValidator;
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

        if (command.IsBta)
        {
            var eligibility = await _btaEligibilityValidator.ValidateAsync(
                participation, cancellationToken);
            if (eligibility.IsFailure)
                return eligibility;
        }

        var result = participation.MarkAbsent(
            command.IsBta ? ParticipationCondition.Bta : null);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
