using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.GetById;

public sealed class GetTrainingByIdQueryHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingRepository _trainingRepository;

    public GetTrainingByIdQueryHandler(
        IActorAuthorizationService authorizationService,
        ITrainingRepository trainingRepository)
    {
        _authorizationService = authorizationService;
        _trainingRepository = trainingRepository;
    }

    public async Task<Result<TrainingDetails>> Handle(
        GetTrainingByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Trainings.GetById,
                cancellationToken);

        if (authorizationResult.IsFailure)
            return Result<TrainingDetails>.Failure(
                authorizationResult.Error!);

        Training? training =
            await _trainingRepository.GetByIdAsync(
                query.TrainingId,
                cancellationToken);

        if (training is null)
            return Result<TrainingDetails>.Failure(
                GetTrainingByIdErrors.NotFound);

        return Result<TrainingDetails>.Success(
            new TrainingDetails(
                training.TrainingId.Value,
                training.Title.Value,
                training.Description.Value,
                training.Location.Value,
                training.Schedule.Date,
                training.Schedule.StartTime,
                training.Schedule.EndTime,
                training.Status,
                training.SeasonOrganization.SeasonId.Value,
                training.SeasonOrganization.OrganizationId.Value,
                training.SeasonTeamId?.Value));
    }
}
