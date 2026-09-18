using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class TrainingTypeRepository : ITrainingTypeRepository
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public TrainingTypeRepository(ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TrainingType?> GetByIdAsync(
        TrainingTypeId id,
        CancellationToken cancellationToken = default) =>
        _dbContext.Set<TrainingType>()
            .SingleOrDefaultAsync(
                trainingType => EF.Property<Guid>(trainingType, "Id") == id.Value,
                cancellationToken);

    public async Task AddAsync(
        TrainingType trainingType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trainingType);
        await _dbContext.Set<TrainingType>().AddAsync(
            trainingType,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingType>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TrainingType> query = _dbContext.Set<TrainingType>();
        if (activeOnly)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code.Value)
            .ToListAsync(cancellationToken);
    }
}
