using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Scouting;

public interface IScoutingCandidateRepository
{
    Task AddAsync(ScoutingCandidate candidate, CancellationToken cancellationToken = default);
    Task<ScoutingCandidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdentityNumberAsync(IdentityNumber identityNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdentityNumberAsync(IdentityNumber identityNumber, Guid excludeCandidateId, CancellationToken cancellationToken = default);
    void RefreshIdentityKey(ScoutingCandidate candidate);
    Task<IReadOnlyList<ScoutingCandidate>> ListAsync(CancellationToken cancellationToken = default);
}
