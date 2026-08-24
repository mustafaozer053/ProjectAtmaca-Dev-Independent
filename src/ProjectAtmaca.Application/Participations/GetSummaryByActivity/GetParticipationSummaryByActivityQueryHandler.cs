using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.GetSummaryByActivity;

public sealed class GetParticipationSummaryByActivityQueryHandler
{
    private readonly IParticipationReader
        _reader;

    public GetParticipationSummaryByActivityQueryHandler(
        IParticipationReader reader)
    {
        _reader =
            reader;
    }

    public async Task<Result<ParticipationActivitySummary>>
        Handle(
            GetParticipationSummaryByActivityQuery query,
            CancellationToken cancellationToken = default)
    {
        ParticipationActivitySummary summary =
            await _reader.GetSummaryByActivityAsync(
                query.ActivityReference,
                cancellationToken);

        return Result<ParticipationActivitySummary>
            .Success(
                summary);
    }
}
