using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.GetById;

public sealed class GetParticipationByIdQueryHandler
{
    private readonly IParticipationReader _reader;

    public GetParticipationByIdQueryHandler(
        IParticipationReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<ParticipationDetails>> Handle(
        GetParticipationByIdQuery query,
        CancellationToken cancellationToken = default)
    {
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
