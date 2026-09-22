using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Infrastructure.Persistence.Readers;

public sealed class AtmacaCardReader
    : IAtmacaCardReader
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public AtmacaCardReader(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, AtmacaCardSummary>> GetSummariesAsync(
        IReadOnlyCollection<Guid> atmacaCardIds,
        CancellationToken cancellationToken = default)
    {
        if (atmacaCardIds.Count == 0)
            return new Dictionary<Guid, AtmacaCardSummary>();

        var query =
            from card in _dbContext.Set<AtmacaCard>().AsNoTracking()
            join person in _dbContext.Set<Person>().AsNoTracking()
                on card.PersonId equals person.Id
            where atmacaCardIds.Contains(card.Id)
            select new AtmacaCardSummary(
                card.Id,
                person.Id,
                person.Name.FullName,
                card.CardNumber.Value);

        var summaries = await query.ToListAsync(cancellationToken);

        return summaries.ToDictionary(
            x => x.AtmacaCardId,
            x => x);
    }

    public async Task<IReadOnlyList<AtmacaCardSummary>> SearchAsync(
        string search,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = search.Trim();
        if (normalizedSearch.Length < 2)
            return [];

        limit = Math.Clamp(limit, 1, 50);

        var matches = await (
            from card in _dbContext.Set<AtmacaCard>().AsNoTracking()
            join person in _dbContext.Set<Person>().AsNoTracking()
                on card.PersonId equals person.Id
            select new { card, person })
            .ToListAsync(cancellationToken);

        return matches
            .Where(x =>
                x.person.Name.FullName.Contains(
                    normalizedSearch,
                    StringComparison.OrdinalIgnoreCase)
                || x.card.CardNumber.Value.Contains(
                    normalizedSearch,
                    StringComparison.OrdinalIgnoreCase))
            .Select(x => new AtmacaCardSummary(
                x.card.Id,
                x.person.Id,
                x.person.Name.FullName,
                x.card.CardNumber.Value))
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.CardNumber)
            .Take(limit)
            .ToList();
    }
}
