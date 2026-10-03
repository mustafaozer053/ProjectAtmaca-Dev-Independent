namespace ProjectAtmaca.Domain.Positions;

public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Position>> ListBySportAsync(
        string sportName,
        bool activeOnly,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsBySportAndCodeAsync(
        string sportName,
        string code,
        CancellationToken cancellationToken = default);
    Task AddAsync(Position position, CancellationToken cancellationToken = default);
}
