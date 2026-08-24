using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Application.Participations;

namespace ProjectAtmaca.Application
    .Participations.ListByActivity;

public sealed class ListParticipationsByActivityQueryHandler
{
    private readonly IParticipationReader
        _reader;

    public ListParticipationsByActivityQueryHandler(
        IParticipationReader reader)
    {
        _reader =
            reader;
    }

    public async Task<Result<IReadOnlyList<ParticipationListItem>>>
        Handle(
            ListParticipationsByActivityQuery query,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ParticipationListItem> participations =
            await _reader.ListByActivityAsync(
                query.ActivityReference,
                cancellationToken);

        return Result<IReadOnlyList<ParticipationListItem>>
            .Success(
                participations);
    }
}
