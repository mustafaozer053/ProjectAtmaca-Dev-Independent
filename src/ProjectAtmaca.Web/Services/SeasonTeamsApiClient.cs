using System.Net.Http.Json;

namespace ProjectAtmaca.Web.Services;

// Thin typed HttpClient wrapper around the ProjectAtmaca API's
// season-teams endpoints. Registered via AddHttpClient in Program.cs
// with its BaseAddress configured from ProjectAtmacaApi:BaseUrl.
public sealed class SeasonTeamsApiClient
{
    private readonly HttpClient _httpClient;

    public SeasonTeamsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SeasonTeamResponse>> GetSeasonTeamsAsync(
        CancellationToken cancellationToken = default)
    {
        var teams = await _httpClient.GetFromJsonAsync<
            List<SeasonTeamResponse>>(
            "api/season-teams",
            cancellationToken);

        return teams ?? [];
    }

    public async Task<SeasonTeamRosterViewResponse?> GetRosterViewAsync(
        Guid seasonTeamId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<
            SeasonTeamRosterViewResponse>(
            $"api/season-teams/{seasonTeamId:D}/roster-view",
            cancellationToken);
    }

    public async Task<IReadOnlyList<AtmacaCardSummary>> SearchCardsAsync(
        string search,
        CancellationToken cancellationToken = default)
    {
        var results = await _httpClient.GetFromJsonAsync<
            List<AtmacaCardSummary>>(
            $"api/atmaca-cards?search={Uri.EscapeDataString(search)}",
            cancellationToken);

        return results ?? [];
    }

    public async Task<bool> AddMembershipAsync(
        Guid seasonTeamId,
        Guid atmacaCardId,
        DateTime startDate,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"api/season-teams/{seasonTeamId:D}/memberships",
            new { AtmacaCardId = atmacaCardId, StartDate = startDate },
            cancellationToken);

        return response.IsSuccessStatusCode;
    }
}
