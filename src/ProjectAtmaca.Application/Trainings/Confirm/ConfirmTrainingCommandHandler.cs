using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.Confirm;

public sealed class ConfirmTrainingCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingRepository _trainingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmTrainingCommandHandler(
        IActorAuthorizationService authorizationService,
        ITrainingRepository trainingRepository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _trainingRepository = trainingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        ConfirmTrainingCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Trainings.Confirm,
                cancellationToken);

        if (authorizationResult.IsFailure)
            return authorizationResult;

        Training? training =
            await _trainingRepository.GetByIdAsync(
                command.TrainingId,
                cancellationToken);

        if (training is null)
            return Result.Failure(ConfirmTrainingErrors.NotFound);

        training.Confirm();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
