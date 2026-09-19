using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.List;

public sealed class ListTrainingsQueryHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingRepository _repository;

    public ListTrainingsQueryHandler(
        IActorAuthorizationService authorizationService,
        ITrainingRepository repository)
    {
        _authorizationService = authorizationService;
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TrainingListItem>>> Handle(
        ListTrainingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FromDate.HasValue &&
            query.ToDate.HasValue &&
            query.FromDate.Value > query.ToDate.Value)
        {
            return Result<IReadOnlyList<TrainingListItem>>.Failure(
                TrainingListErrors.InvalidDateRange);
        }

        Result authorization = await _authorizationService.AuthorizeAsync(
            Permissions.Trainings.List,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<TrainingListItem>>.Failure(
                authorization.Error!);

        IReadOnlyList<Training> trainings = await _repository.ListAsync(
            cancellationToken);

        return Result<IReadOnlyList<TrainingListItem>>.Success(
            trainings
                .Where(training =>
                    (!query.FromDate.HasValue ||
                        training.Schedule.Date >= query.FromDate.Value) &&
                    (!query.ToDate.HasValue ||
                        training.Schedule.Date <= query.ToDate.Value) &&
                    (!query.SeasonId.HasValue ||
                        training.SeasonOrganization.SeasonId.Value ==
                        query.SeasonId.Value) &&
                    (!query.OrganizationId.HasValue ||
                        training.SeasonOrganization.OrganizationId.Value ==
                        query.OrganizationId.Value) &&
                    (!query.Status.HasValue ||
                        training.Status == query.Status.Value))
                .OrderBy(training => training.Schedule.Date)
                .ThenBy(training => training.Schedule.StartTime)
                .Select(training => new TrainingListItem(
                    training.TrainingId.Value,
                    training.Title.Value,
                    training.Location.Value,
                    training.Schedule.Date,
                    training.Schedule.StartTime,
                    training.Schedule.EndTime,
                    training.Status,
                    training.SeasonOrganization.SeasonId.Value,
                    training.SeasonOrganization.OrganizationId.Value))
                .ToList());
    }
}
