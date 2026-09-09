using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Decisions.ListApplicationHistory;

public sealed class ListDecisionApplicationHistoryQueryHandler
{
    private readonly IActorAuthorizationService
        _authorizationService;

    private readonly IDecisionApplicationReader _reader;

    public ListDecisionApplicationHistoryQueryHandler(
        IActorAuthorizationService authorizationService,
        IDecisionApplicationReader reader)
    {
        _authorizationService =
            authorizationService;

        _reader =
            reader;
    }

    public async Task<Result<IReadOnlyList<DecisionApplicationHistoryItem>>>
        Handle(
            ListDecisionApplicationHistoryQuery query,
            CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Decisions
                    .ListApplicationHistory,
                cancellationToken);

        if (authorizationResult.IsFailure)
        {
            return Result<IReadOnlyList<DecisionApplicationHistoryItem>>
                .Failure(
                    authorizationResult.Error!);
        }

        IReadOnlyList<DecisionApplicationHistoryItem> items =
            await _reader.ListHistoryByDecisionAsync(
                query.DecisionId,
                cancellationToken);

        return Result<IReadOnlyList<DecisionApplicationHistoryItem>>
            .Success(
                items);
    }
}
