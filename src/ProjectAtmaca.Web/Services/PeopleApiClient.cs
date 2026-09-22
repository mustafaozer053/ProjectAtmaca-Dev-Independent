using System.Net.Http.Json;

namespace ProjectAtmaca.Web.Services;

public sealed class PeopleApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<AtmacaCardSummary>> ListAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(search)
            ? "api/atmaca-cards"
            : $"api/atmaca-cards?search={Uri.EscapeDataString(search.Trim())}";
        var results = await httpClient.GetFromJsonAsync<List<AtmacaCardSummary>>(
            path, cancellationToken);
        return results ?? [];
    }
}
