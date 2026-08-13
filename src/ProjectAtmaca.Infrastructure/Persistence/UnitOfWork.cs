using ProjectAtmaca.Application
    .Abstractions.Persistence;

namespace ProjectAtmaca.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ProjectAtmacaDbContext _dbContext;

    public UnitOfWork(
        ProjectAtmacaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
