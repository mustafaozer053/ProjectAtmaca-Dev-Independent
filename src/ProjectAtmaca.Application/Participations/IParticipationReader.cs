using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Application.Participations;

public interface IParticipationReader
{
    Task<ParticipationDetails?> GetByIdAsync(
        Guid participationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParticipationListItem>>
        ListByActivityAsync(
            ActivityReference activityReference,
            CancellationToken cancellationToken = default);

    Task<ParticipationActivitySummary>
        GetSummaryByActivityAsync(
            ActivityReference activityReference,
            CancellationToken cancellationToken = default);

    Task<ParticipationHistoryPage>
        ListHistoryByAtmacaCardAsync(
            AtmacaCardId atmacaCardId,
            int pageSize,
            ParticipationHistoryCursor? cursor,
            CancellationToken cancellationToken = default);

}
