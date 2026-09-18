using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Infrastructure.Persistence.Repositories;

public sealed class TrainingRepository : ITrainingRepository
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public TrainingRepository(ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Training?> GetByIdAsync(
        TrainingId id,
        CancellationToken cancellationToken = default) =>
        _dbContext.Set<Training>()
            .SingleOrDefaultAsync(
                training => EF.Property<Guid>(training, "Id") == id.Value,
                cancellationToken);

    public async Task AddAsync(
        Training training,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(training);
        await _dbContext.Set<Training>().AddAsync(training, cancellationToken);
    }
}
