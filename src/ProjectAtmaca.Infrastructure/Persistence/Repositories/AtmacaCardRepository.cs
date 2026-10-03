using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class AtmacaCardRepository(ProjectAtmacaDbContext dbContext)
    : IAtmacaCardRepository
{
    public Task<AtmacaCard?> GetByIdAsync(
        AtmacaCardId id,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<AtmacaCard>()
            .Include(x => x.SportsProfiles)
                .ThenInclude(x => x.Positions)
            .Include(x => x.Measurements)
            .Include(x => x.Documents)
            .SingleOrDefaultAsync(
            card => EF.Property<Guid>(card, "Id") == id.Value,
            cancellationToken);

}
