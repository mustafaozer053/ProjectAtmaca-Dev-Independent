using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Application.Participations;

namespace ProjectAtmaca.Application
    .Participations.GetById;

public sealed class GetParticipationByIdQueryHandler
{
    private readonly IActorAuthorizationService
        _authorizationService;

    private readonly IParticipationReader _reader;

    public GetParticipationByIdQueryHandler(
        IActorAuthorizationService authorizationService,
        IParticipationReader reader)
    {
        _authorizationService =
            authorizationService;

        _reader = reader;
    }

    public async Task<Result<ParticipationDetails>> Handle(
        GetParticipationByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        Result authorizationResult =
            await _authorizationService.AuthorizeAsync(
                Permissions.Participations.GetById,
                cancellationToken);

        if (authorizationResult.IsFailure)
        {
            return Result<ParticipationDetails>.Failure(
                authorizationResult.Error!);
        }

        ParticipationDetails? participation =
            await _reader.GetByIdAsync(
                query.ParticipationId,
                cancellationToken);

        if (participation is null)
        {
            return Result<ParticipationDetails>.Failure(
                GetParticipationByIdErrors.NotFound);
        }

        return Result<ParticipationDetails>.Success(
            participation);
    }
}
