using System.Net.Http.Json;

namespace ProjectAtmaca.Web.Services;

public sealed class FixturesApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<FixtureResponse>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<FixtureResponse>>(
            "api/fixtures", cancellationToken) ?? [];

    public async Task CreateAsync(
        Guid seasonTeamId,
        int type,
        string opponent,
        DateOnly date,
        TimeOnly startTime,
        string venue,
        int venueSide,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/fixtures",
            Request(seasonTeamId, type, opponent, date, startTime, venue, venueSide, notes),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateAsync(
        Guid fixtureId,
        Guid seasonTeamId,
        int type,
        string opponent,
        DateOnly date,
        TimeOnly startTime,
        string venue,
        int venueSide,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/fixtures/{fixtureId:D}",
            Request(seasonTeamId, type, opponent, date, startTime, venue, venueSide, notes),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CancelAsync(Guid fixtureId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/fixtures/{fixtureId:D}/cancel", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CompleteAsync(Guid fixtureId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/fixtures/{fixtureId:D}/complete", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ReopenForCorrectionAsync(
        Guid fixtureId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/fixtures/{fixtureId:D}/reopen-for-correction",
            new { Reason = reason },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<FixtureDetailsResponse?> GetDetailsAsync(
        Guid fixtureId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<FixtureDetailsResponse>(
            $"api/fixtures/{fixtureId:D}", cancellationToken);

    public async Task UpdateMatchDetailsAsync(
        Guid fixtureId,
        int durationMinutes,
        string? referee,
        string? matchNotes,
        IReadOnlyList<FixtureSquadMemberInput> squad,
        IReadOnlyList<FixtureMatchEventInput> matchEvents,
        IReadOnlyList<FixtureScoreEventInput> scoreEvents,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/fixtures/{fixtureId:D}/match-details",
            new
            {
                DurationMinutes = durationMinutes,
                Referee = referee,
                MatchNotes = matchNotes,
                Squad = squad,
                MatchEvents = matchEvents,
                ScoreEvents = scoreEvents
            },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static object Request(
        Guid seasonTeamId, int type, string opponent, DateOnly date,
        TimeOnly startTime, string venue, int venueSide, string? notes) =>
        new { SeasonTeamId = seasonTeamId, Type = type, Opponent = opponent,
            Date = date, StartTime = startTime, Venue = venue,
            VenueSide = venueSide, Notes = notes };

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;
        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(
            cancellationToken: cancellationToken);
        throw new HttpRequestException(
            problem?.Detail ?? $"Müsabaka kaydı reddedildi (HTTP {(int)response.StatusCode}).");
    }

    private sealed record ApiProblem(string? Detail);
}

public sealed record FixtureResponse(
    Guid Id,
    Guid SeasonTeamId,
    int Type,
    string Opponent,
    DateOnly Date,
    TimeOnly StartTime,
    string Venue,
    int VenueSide,
    string? Notes,
    int Status);

public sealed record FixtureDetailsResponse(
    FixtureResponse Fixture,
    int? DurationMinutes,
    string? Referee,
    string? MatchNotes,
    int OurScore,
    int OpponentScore,
    IReadOnlyList<FixtureSquadMemberInput> SquadMembers,
    IReadOnlyList<FixtureMatchEventInput> MatchEvents,
    IReadOnlyList<FixtureScoreEventInput> ScoreEvents,
    IReadOnlyList<FixtureCorrectionDetailsResponse> Corrections,
    IReadOnlyList<FixturePlayerStatisticsResponse> PlayerStatistics);

public sealed record FixturePlayerStatisticsResponse(
    Guid AtmacaCardId,
    int Role,
    bool Started,
    int MinutesPlayed,
    int Goals,
    int Assists,
    int YellowCards,
    int RedCards);

public sealed record FixtureCorrectionDetailsResponse(
    string Reason,
    DateTime ReopenedAtUtc,
    Guid ReopenedByActorId);

public sealed record FixtureSquadMemberInput(
    Guid AtmacaCardId,
    int Role);

public sealed record FixtureMatchEventInput(
    int Type,
    int Minute,
    Guid? AtmacaCardId,
    Guid? RelatedAtmacaCardId);

public sealed record FixtureScoreEventInput(
    int Side,
    string ScoreTypeCode,
    int ScoreValue,
    int? Minute,
    Guid? AtmacaCardId,
    Guid? AssistAtmacaCardId = null);
