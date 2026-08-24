using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;

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
    public async Task<IReadOnlyList<ParticipationListItem>>
        ListByActivityAsync(
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
    {
        var rows =
            await _dbContext.Participations
                .AsNoTracking()
                .Where(
                    item =>
                        item.ActivityReference ==
                        activityReference)
                .Select(
                    item =>
                        new
                        {
                            Id =
                                EF.Property<Guid>(
                                    item,
                                    "Id"),

                            item.AtmacaCardId,
                            item.Status,
                            item.Condition,
                            item.JoinedAt,
                            item.LeftAt
                        })
                .ToListAsync(
                    cancellationToken);

        return rows
            .Select(
                item =>
                    new ParticipationListItem(
                        item.Id,
                        item.AtmacaCardId.Value,
                        item.Status,
                        item.Condition?.Code,
                        item.JoinedAt,
                        item.LeftAt))
            .OrderBy(
                item =>
                    item.Id)
            .ToList();
    }

    public async Task<ParticipationActivitySummary>
        GetSummaryByActivityAsync(
            ActivityReference activityReference,
            CancellationToken cancellationToken = default)
    {
        var summary =
            await _dbContext.Participations
                .AsNoTracking()
                .Where(
                    participation =>
                        participation.ActivityReference ==
                        activityReference)
                .GroupBy(
                    _ => 1)
                .Select(
                    group =>
                        new ParticipationActivitySummary(
                            group.Count(),
                            group.Count(
                                participation =>
                                    participation.Status ==
                                    ParticipationStatus.NotRecorded),
                            group.Count(
                                participation =>
                                    participation.Status ==
                                    ParticipationStatus.Present),
                            group.Count(
                                participation =>
                                    participation.Status ==
                                    ParticipationStatus.Absent)))
                .SingleOrDefaultAsync(
                    cancellationToken);

        return summary
            ?? new ParticipationActivitySummary(
                Total: 0,
                NotRecorded: 0,
                Present: 0,
                Absent: 0);
    }
}
