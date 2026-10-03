namespace ProjectAtmaca.Domain.Fixtures;

public interface IFixtureRepository
{
    Task AddAsync(Fixture fixture, CancellationToken cancellationToken = default);
    Task<Fixture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fixture>> ListAsync(CancellationToken cancellationToken = default);
}
