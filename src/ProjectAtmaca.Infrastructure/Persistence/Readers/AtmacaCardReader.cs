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
}
