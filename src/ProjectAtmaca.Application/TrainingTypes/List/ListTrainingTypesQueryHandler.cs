using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.TrainingTypes.List;

public sealed class ListTrainingTypesQueryHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly ITrainingTypeRepository _repository;

    public ListTrainingTypesQueryHandler(
        IActorAuthorizationService authorizationService,
        ITrainingTypeRepository repository)
    {
        _authorizationService = authorizationService;
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TrainingTypeListItem>>> Handle(
        ListTrainingTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        Result authorization = await _authorizationService.AuthorizeAsync(
            Permissions.TrainingTypes.List,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<TrainingTypeListItem>>.Failure(
                authorization.Error!);

        IReadOnlyList<TrainingType> values = await _repository.ListAsync(
            query.ActiveOnly,
            cancellationToken);
        return Result<IReadOnlyList<TrainingTypeListItem>>.Success(
            values.Select(x => new TrainingTypeListItem(
                x.TrainingTypeId.Value,
                x.Code.Value,
                x.Name.Value,
                x.Description.Value,
                x.DisplayOrder,
                x.IsActive)).ToList());
    }
}
