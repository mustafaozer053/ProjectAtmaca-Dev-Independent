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
}
