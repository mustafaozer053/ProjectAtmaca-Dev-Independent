using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Domain.AtmacaCards;

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
                        activityReference &&
                        EF.Property<ParticipationCondition?>(
                            participation,
                            nameof(Participation.Condition)) !=
                            ParticipationCondition.Bta)
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

    public async Task<ParticipationHistoryPage>
        ListHistoryByAtmacaCardAsync(
            AtmacaCardId atmacaCardId,
            int pageSize,
            ParticipationHistoryCursor? cursor,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.Participations
                .AsNoTracking()
                .Where(
                    participation =>
                        participation.AtmacaCardId ==
                        atmacaCardId);

        if (cursor is not null)
        {
            query =
                query.Where(
                    participation =>
                        participation.CreatedAtUtc <
                            cursor.CreatedAtUtc ||
                        (
                            participation.CreatedAtUtc ==
                                cursor.CreatedAtUtc &&
                            EF.Property<Guid>(
                                    participation,
                                    "Id")
                                .CompareTo(
                                    cursor.ParticipationId) <
                                0
                        ));
        }

        var rows =
            await query
                .OrderByDescending(
            participation =>
                participation.CreatedAtUtc)
        .ThenByDescending(
            participation =>
                EF.Property<Guid>(
                    participation,
                    "Id"))
        .Take(
            pageSize + 1)
        .Select(
            participation =>
                new
                {
                    Id =
                        EF.Property<Guid>(
                            participation,
                            "Id"),

                    participation.ActivityReference,
                    participation.Status,
                    participation.Condition,
                    participation.JoinedAt,
                    participation.LeftAt,
                    participation.CreatedAtUtc
                })
        .ToListAsync(
            cancellationToken);



        bool hasMore =
            rows.Count >
            pageSize;

        var pageRows =
            rows
                .Take(
                    pageSize)
                .ToList();

        IReadOnlyList<ParticipationHistoryItem> items =
            pageRows
                .Select(
                    row =>
                        new ParticipationHistoryItem(
                            row.Id,
                            row.ActivityReference,
                            row.Status,
                            row.Condition?.Code,
                            row.JoinedAt,
                            row.LeftAt,
                            DateTime.SpecifyKind(
                                row.CreatedAtUtc,
                                DateTimeKind.Utc)))
                .ToList();

        ParticipationHistoryCursor? nextCursor =
            null;

        if (hasMore &&
            pageRows.Count > 0)
        {
            var lastRow =
                pageRows[^1];

            nextCursor =
                new ParticipationHistoryCursor(
                    atmacaCardId,
                    DateTime.SpecifyKind(
                        lastRow.CreatedAtUtc,
                        DateTimeKind.Utc),
                    lastRow.Id);
        }

        return new ParticipationHistoryPage(
            items,
            nextCursor);
    }
}
