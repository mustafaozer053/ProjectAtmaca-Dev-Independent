using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.GetSummaryByActivity;

public sealed class GetParticipationSummaryByActivityQueryHandler
{
    private readonly IActorAuthorizationService
        _authorizationService;

    private readonly IParticipationReader
        _reader;

    public GetParticipationSummaryByActivityQueryHandler(
        IActorAuthorizationService authorizationService,
        IParticipationReader reader)
    {
        _authorizationService =
            authorizationService;

        _reader =
            reader;
    }

    public async Task<Result<ParticipationActivitySummary>>
        Handle(
            GetParticipationSummaryByActivityQuery query,
            CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Participations
                    .GetSummaryByActivity,
                cancellationToken);

        if (authorizationResult.IsFailure)
        {
            return Result<ParticipationActivitySummary>
                .Failure(
                    authorizationResult.Error!);
        }

        ParticipationActivitySummary summary =
            await _reader.GetSummaryByActivityAsync(
                query.ActivityReference,
                cancellationToken);

        return Result<ParticipationActivitySummary>
            .Success(
                summary);
    }
}
