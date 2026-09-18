using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.Create;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.Reschedule;

public sealed class RescheduleTrainingCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingRepository _trainingRepository;
    private readonly ITrainingTypeRepository _trainingTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleTrainingCommandHandler(
        IActorAuthorizationService authorizationService,
        ITrainingRepository trainingRepository,
        ITrainingTypeRepository trainingTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _trainingRepository = trainingRepository;
        _trainingTypeRepository = trainingTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RescheduleTrainingCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Trainings.Reschedule,
                cancellationToken);

        if (authorizationResult.IsFailure)
            return authorizationResult;

        Training? training = await _trainingRepository.GetByIdAsync(
            command.TrainingId,
            cancellationToken);

        if (training is null)
            return Result.Failure(RescheduleTrainingErrors.NotFound);

        if (command.Assignments is null)
            return Result.Failure(TrainingCreationErrors.AssignmentsRequired);

        var assignments = new List<TrainingTypeAssignment>();
        foreach (TrainingTypeAssignmentInput input in command.Assignments)
        {
            if (input.TrainingTypeId == Guid.Empty)
                return Result.Failure(TrainingCreationErrors.TrainingTypeRequired);

            TrainingType? trainingType =
                await _trainingTypeRepository.GetByIdAsync(
                    TrainingTypeId.From(input.TrainingTypeId),
                    cancellationToken);

            if (trainingType is null)
                return Result.Failure(TrainingCreationErrors.TrainingTypeNotFound);

            if (!trainingType.IsActive)
                return Result.Failure(TrainingCreationErrors.TrainingTypeInactive);

            Result<TrainingTypeDuration> durationResult =
                TrainingTypeDuration.Create(input.DurationMinutes);
            if (durationResult.IsFailure)
                return Result.Failure(durationResult.Error!);

            assignments.Add(
                TrainingTypeAssignment.Create(
                    TrainingTypeId.From(input.TrainingTypeId),
                    durationResult.Value!));
        }

        Result<TrainingSchedule> scheduleResult = TrainingSchedule.Create(
            command.Date,
            command.StartTime,
            command.EndTime);
        if (scheduleResult.IsFailure)
            return Result.Failure(scheduleResult.Error!);

        Result rescheduleResult = training.Reschedule(
            scheduleResult.Value!,
            assignments);
        if (rescheduleResult.IsFailure)
            return rescheduleResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
