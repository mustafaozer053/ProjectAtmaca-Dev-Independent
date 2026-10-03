using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class PositionRepository(ProjectAtmacaDbContext dbContext) : IPositionRepository
{
    public Task<Position?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Positions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Position>> ListBySportAsync(
        string sportName,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Position> query = dbContext.Positions
            .Where(x => x.SportName.ToUpper() == sportName.Trim().ToUpper());
        if (activeOnly)
            query = query.Where(x => x.IsActive);

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsBySportAndCodeAsync(
        string sportName,
        string code,
        CancellationToken cancellationToken = default) =>
        dbContext.Positions.AnyAsync(
            x => x.SportName.ToUpper() == sportName.Trim().ToUpper() &&
                 x.Code == code.Trim().ToUpper(),
            cancellationToken);

    public async Task AddAsync(
        Position position,
        CancellationToken cancellationToken = default) =>
        await dbContext.Positions.AddAsync(position, cancellationToken);
}
