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
}
