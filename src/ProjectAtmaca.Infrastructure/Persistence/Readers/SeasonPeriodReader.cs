using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Seasons;

namespace ProjectAtmaca.Infrastructure.Persistence.Readers;

public sealed class SeasonPeriodReader(ProjectAtmacaDbContext dbContext)
    : ISeasonPeriodReader
{
    public async Task<DateRange?> GetPeriodAsync(
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        Season? season = await dbContext.Set<Season>()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == seasonId, cancellationToken);
        return season?.Period;
    }
}
