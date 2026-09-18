using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.Cancel;

public sealed class CancelTrainingCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingRepository _trainingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelTrainingCommandHandler(
        IActorAuthorizationService authorizationService,
        ITrainingRepository trainingRepository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _trainingRepository = trainingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        CancelTrainingCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Trainings.Cancel,
                cancellationToken);

        if (authorizationResult.IsFailure)
            return authorizationResult;

        Training? training =
            await _trainingRepository.GetByIdAsync(
                command.TrainingId,
                cancellationToken);

        if (training is null)
            return Result.Failure(CancelTrainingErrors.NotFound);

        Result cancelResult = training.Cancel();
        if (cancelResult.IsFailure)
            return cancelResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
