using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Repositories;

public sealed class ParticipationRepository
    : IParticipationRepository
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public ParticipationRepository(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Participation?> GetByIdAsync(
        ParticipationId id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Participations
            .FindAsync(
                [id.Value],
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        AtmacaCardId atmacaCardId,
        ActivityReference activityReference,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Participations
            .AnyAsync(
                participation =>
                    participation.AtmacaCardId ==
                        atmacaCardId &&
                    participation.ActivityReference ==
                        activityReference,
                cancellationToken);
    }

    public async Task AddAsync(
        Participation participation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            participation);

        await _dbContext.Participations
            .AddAsync(
                participation,
                cancellationToken);
    }
}
