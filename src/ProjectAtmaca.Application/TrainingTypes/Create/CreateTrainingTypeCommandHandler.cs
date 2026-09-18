using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.TrainingTypes.Create;

public sealed class CreateTrainingTypeCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTrainingTypeCommandHandler(
        IActorAuthorizationService authorizationService,
        ITrainingTypeRepository repository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TrainingTypeId>> Handle(
        CreateTrainingTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorization = await _authorizationService.AuthorizeAsync(
            Permissions.TrainingTypes.Create,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<TrainingTypeId>.Failure(authorization.Error!);

        Result<TrainingTypeCode> code = TrainingTypeCode.Create(command.Code);
        if (code.IsFailure)
            return Result<TrainingTypeId>.Failure(code.Error!);
        Result<TrainingTypeName> name = TrainingTypeName.Create(command.Name);
        if (name.IsFailure)
            return Result<TrainingTypeId>.Failure(name.Error!);
        Result<TrainingTypeDescription> description =
            TrainingTypeDescription.Create(command.Description);
        if (description.IsFailure)
            return Result<TrainingTypeId>.Failure(description.Error!);

        Result<TrainingType> trainingType = TrainingType.Create(
            code.Value!, name.Value!, description.Value!, command.DisplayOrder);
        if (trainingType.IsFailure)
            return Result<TrainingTypeId>.Failure(trainingType.Error!);

        await _repository.AddAsync(trainingType.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TrainingTypeId>.Success(trainingType.Value!.TrainingTypeId);
    }
}
