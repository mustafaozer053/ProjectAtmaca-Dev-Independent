using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Domain.Participations;

public interface IParticipationRepository
{
    Task<Participation?> GetByIdAsync(
        ParticipationId id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        AtmacaCardId atmacaCardId,
        ActivityReference activityReference,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Participation participation,
        CancellationToken cancellationToken = default);
}
