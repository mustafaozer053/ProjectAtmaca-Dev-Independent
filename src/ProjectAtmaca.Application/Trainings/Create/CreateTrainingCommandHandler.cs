using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.Trainings.Create;

public sealed class CreateTrainingCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingRepository _trainingRepository;
    private readonly ITrainingTypeRepository _trainingTypeRepository;
    private readonly ISeasonTeamRepository? _seasonTeamRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTrainingCommandHandler(
        IActorAuthorizationService authorizationService,
        ITrainingRepository trainingRepository,
        ITrainingTypeRepository trainingTypeRepository,
        IUnitOfWork unitOfWork,
        ISeasonTeamRepository? seasonTeamRepository = null)
    {
        _authorizationService = authorizationService;
        _trainingRepository = trainingRepository;
        _trainingTypeRepository = trainingTypeRepository;
        _seasonTeamRepository = seasonTeamRepository;
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

            TrainingType? trainingType =
                await _trainingTypeRepository.GetByIdAsync(
                    TrainingTypeId.From(input.TrainingTypeId),
                    cancellationToken);

            if (trainingType is null)
                return Result<TrainingId>.Failure(
                    TrainingCreationErrors.TrainingTypeNotFound);

            if (!trainingType.IsActive)
                return Result<TrainingId>.Failure(
                    TrainingCreationErrors.TrainingTypeInactive);

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
            command.SeasonTeamId == Guid.Empty
                ? Training.Create(
                SeasonOrganization.Create(
                    SeasonId.From(command.SeasonId),
                    OrganizationId.From(command.OrganizationId)),
                titleResult.Value!,
                descriptionResult.Value!,
                locationResult.Value!,
                scheduleResult.Value!,
                assignments)
                : await CreateForSeasonTeamAsync(
                    command,
                    titleResult.Value!,
                    descriptionResult.Value!,
                    locationResult.Value!,
                    scheduleResult.Value!,
                    assignments,
                    cancellationToken);

        if (trainingResult.IsFailure)
            return Result<TrainingId>.Failure(trainingResult.Error!);

        Training training = trainingResult.Value!;
        await _trainingRepository.AddAsync(training, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TrainingId>.Success(training.TrainingId);
    }

    private async Task<Result<Training>> CreateForSeasonTeamAsync(
        CreateTrainingCommand command,
        TrainingTitle title,
        TrainingDescription description,
        TrainingLocation location,
        TrainingSchedule schedule,
        IReadOnlyCollection<TrainingTypeAssignment> assignments,
        CancellationToken cancellationToken)
    {
        if (_seasonTeamRepository is null)
            return Result<Training>.Failure(
                TrainingCreationErrors.SeasonTeamRequired);

        SeasonTeam? seasonTeam = await _seasonTeamRepository.GetByIdAsync(
            SeasonTeamId.From(command.SeasonTeamId),
            cancellationToken);
        if (seasonTeam is null)
            return Result<Training>.Failure(
                TrainingCreationErrors.SeasonTeamNotFound);

        if (seasonTeam.SeasonId.Value != command.SeasonId ||
            seasonTeam.OrganizationId.Value != command.OrganizationId)
        {
            return Result<Training>.Failure(
                TrainingCreationErrors.SeasonTeamContextMismatch);
        }

        var seasonOrganization = SeasonOrganization.Create(
            SeasonId.From(command.SeasonId),
            OrganizationId.From(command.OrganizationId));

        return Training.Create(
            seasonOrganization,
            SeasonTeamId.From(command.SeasonTeamId),
            title,
            description,
            location,
            schedule,
            assignments);
    }
}
