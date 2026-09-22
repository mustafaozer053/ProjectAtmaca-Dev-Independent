namespace ProjectAtmaca.Application.AtmacaCards;

/// <summary>
/// Reads display-oriented AtmacaCard/Person data for other aggregates
/// (e.g. SeasonTeam rosters) that reference an AtmacaCard by id only.
/// This is a read model; it must never be used to author identity data.
/// </summary>
public interface IAtmacaCardReader
{
    Task<IReadOnlyList<AtmacaCardSummary>> ListAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, AtmacaCardSummary>> GetSummariesAsync(
        IReadOnlyCollection<Guid> atmacaCardIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AtmacaCardSummary>> SearchAsync(
        string search,
        int limit = 20,
        CancellationToken cancellationToken = default);
}
