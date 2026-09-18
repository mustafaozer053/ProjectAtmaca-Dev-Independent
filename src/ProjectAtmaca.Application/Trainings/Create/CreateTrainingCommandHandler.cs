using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.Create;

public sealed class CreateTrainingCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingRepository _trainingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTrainingCommandHandler(
        IActorAuthorizationService authorizationService,
        ITrainingRepository trainingRepository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _trainingRepository = trainingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TrainingId>> Handle(
        CreateTrainingCommand command,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Trainings.Create,
                cancellationToken);

        if (authorizationResult.IsFailure)
            return Result<TrainingId>.Failure(authorizationResult.Error!);

        if (command.SeasonId == Guid.Empty ||
            command.OrganizationId == Guid.Empty)
        {
            return Result<TrainingId>.Failure(
                TrainingCreationErrors.OrganizationContextRequired);
        }

        if (command.Assignments is null)
        {
            return Result<TrainingId>.Failure(
                TrainingCreationErrors.AssignmentsRequired);
        }

        var assignments = new List<TrainingTypeAssignment>();
        foreach (TrainingTypeAssignmentInput input in command.Assignments)
        {
            if (input.TrainingTypeId == Guid.Empty)
            {
                return Result<TrainingId>.Failure(
                    TrainingCreationErrors.TrainingTypeRequired);
            }

            Result<TrainingTypeDuration> durationResult =
                TrainingTypeDuration.Create(input.DurationMinutes);

            if (durationResult.IsFailure)
                return Result<TrainingId>.Failure(durationResult.Error!);

            assignments.Add(
                TrainingTypeAssignment.Create(
                    TrainingTypeId.From(input.TrainingTypeId),
                    durationResult.Value!));
        }

        Result<TrainingSchedule> scheduleResult =
            TrainingSchedule.Create(
                command.Date,
                command.StartTime,
                command.EndTime);

        if (scheduleResult.IsFailure)
            return Result<TrainingId>.Failure(scheduleResult.Error!);

        Result<TrainingTitle> titleResult =
            TrainingTitle.Create(command.Title);
        if (titleResult.IsFailure)
            return Result<TrainingId>.Failure(titleResult.Error!);

        Result<TrainingDescription> descriptionResult =
            TrainingDescription.Create(command.Description);
        if (descriptionResult.IsFailure)
            return Result<TrainingId>.Failure(descriptionResult.Error!);

        Result<TrainingLocation> locationResult =
            TrainingLocation.Create(command.Location);
        if (locationResult.IsFailure)
            return Result<TrainingId>.Failure(locationResult.Error!);

        Result<Training> trainingResult =
            Training.Create(
                SeasonOrganization.Create(
                    SeasonId.From(command.SeasonId),
                    OrganizationId.From(command.OrganizationId)),
                titleResult.Value!,
                descriptionResult.Value!,
                locationResult.Value!,
                scheduleResult.Value!,
                assignments);

        if (trainingResult.IsFailure)
            return Result<TrainingId>.Failure(trainingResult.Error!);

        Training training = trainingResult.Value!;
        await _trainingRepository.AddAsync(training, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TrainingId>.Success(training.TrainingId);
    }
}
