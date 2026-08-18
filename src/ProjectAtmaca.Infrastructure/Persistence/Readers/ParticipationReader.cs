using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Participations.GetById;

namespace ProjectAtmaca.Infrastructure.Persistence.Readers;


public sealed class ParticipationReader
    : IParticipationReader
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public ParticipationReader(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParticipationDetails?> GetByIdAsync(
        Guid participationId,
        CancellationToken cancellationToken = default)
    {
        var participation =
            await _dbContext.Participations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == participationId,
                    cancellationToken);

        if (participation is null)
        {
            return null;
        }

        return new ParticipationDetails(
            participation.ParticipationId.Value,
            participation.AtmacaCardId.Value,
            participation.ActivityReference.ActivityType.Value,
            participation.ActivityReference.ActivityId,
            participation.Status,
            participation.Condition?.Code,
            participation.JoinedAt,
            participation.LeftAt,
            participation.Note?.Value);
    }
}
