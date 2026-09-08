using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;

namespace ProjectAtmaca.Application
    .Participations.ListByActivity;

public sealed class ListParticipationsByActivityQueryHandler
{
    private readonly IActorAuthorizationService
        _authorizationService;

    private readonly IParticipationReader
        _reader;

    public ListParticipationsByActivityQueryHandler(
        IActorAuthorizationService authorizationService,
        IParticipationReader reader)
    {
        _authorizationService =
            authorizationService;

        _reader =
            reader;
    }

    public async Task<Result<IReadOnlyList<ParticipationListItem>>>
        Handle(
            ListParticipationsByActivityQuery query,
            CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Participations.ListByActivity,
                cancellationToken);

        if (authorizationResult.IsFailure)
        {
            return Result<IReadOnlyList<ParticipationListItem>>
                .Failure(
                    authorizationResult.Error!);
        }

        IReadOnlyList<ParticipationListItem> participations =
            await _reader.ListByActivityAsync(
                query.ActivityReference,
                cancellationToken);

        return Result<IReadOnlyList<ParticipationListItem>>
            .Success(
                participations);
    }
}
