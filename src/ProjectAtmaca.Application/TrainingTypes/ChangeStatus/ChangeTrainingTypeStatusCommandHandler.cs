using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.TrainingTypes.ChangeStatus;

public sealed class ChangeTrainingTypeStatusCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeTrainingTypeStatusCommandHandler(
        IActorAuthorizationService authorizationService,
        ITrainingTypeRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        ChangeTrainingTypeStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorization = await _authorizationService.AuthorizeAsync(
            Permissions.TrainingTypes.ChangeStatus,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;

        TrainingType? trainingType = await _repository.GetByIdAsync(
            command.TrainingTypeId,
            cancellationToken);
        if (trainingType is null)
            return Result.Failure(TrainingTypeApplicationErrors.NotFound);

        if (command.IsActive)
            trainingType.Activate();
        else
            trainingType.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
