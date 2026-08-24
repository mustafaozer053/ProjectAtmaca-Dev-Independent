using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Domain.Participations;

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
}
