using System.Net.Http.Json;

namespace ProjectAtmaca.Web.Services;

public sealed class ScoutingApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ScoutingCandidateListResponse>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<ScoutingCandidateListResponse>>(
            "api/scouting/candidates", cancellationToken) ?? [];

    public async Task<ScoutingCandidateDetailsResponse?> GetAsync(
        Guid candidateId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<ScoutingCandidateDetailsResponse>(
            $"api/scouting/candidates/{candidateId:D}", cancellationToken);

    public async Task CreateAsync(
        ScoutingCandidateRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/scouting/candidates", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task AddObservationAsync(
        Guid candidateId, ScoutingObservationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/scouting/candidates/{candidateId:D}/observations", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateCandidateAsync(
        Guid candidateId, UpdateScoutingCandidateRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/scouting/candidates/{candidateId:D}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateObservationAsync(
        Guid candidateId, Guid observationId, ScoutingObservationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/scouting/candidates/{candidateId:D}/observations/{observationId:D}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ScoutingRegistrationMatchResponse>> GetRegistrationMatchesAsync(
        Guid candidateId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<ScoutingRegistrationMatchResponse>>(
            $"api/scouting/candidates/{candidateId:D}/registration-matches", cancellationToken) ?? [];

    public async Task LinkRegistrationAsync(
        Guid candidateId, Guid personId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/scouting/candidates/{candidateId:D}/registration", new { PersonId = personId }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ChangeDecisionAsync(
        Guid candidateId, int decision, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/scouting/candidates/{candidateId:D}/decision", new { Decision = decision }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;
        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(
            cancellationToken: cancellationToken);
        throw new HttpRequestException(
            problem?.Detail ?? $"Aday kaydı reddedildi (HTTP {(int)response.StatusCode}).");
    }

    private sealed record ApiProblem(string? Detail);
}

public sealed record ScoutingObservationRequest(
    DateOnly ObservedOn,
    int ObservationType,
    string? ObservedEvent,
    string? ObservedClub,
    string? ObservedTeam,
    int? DominantFoot,
    string? Strengths,
    string? Weaknesses,
    int? Recommendation,
    string? RecommendationNote,
    string ObserverName,
    int? Rating,
    IReadOnlyList<Guid>? PositionIds = null);

public sealed record ScoutingCandidateRequest(
    string Name,
    DateOnly? BirthDate,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Email,
    int SourceType,
    string? ReferrerName,
    string? SourceDescription,
    ScoutingObservationRequest Observation,
    int? IdentityType = null,
    string? IdentityNumber = null,
    string? IdentityCountryCode = null);

public sealed record UpdateScoutingCandidateRequest(
    string Name,
    DateOnly? BirthDate,
    string? PhoneCountryCode,
    string? PhoneNumber,
    string? Email,
    int? IdentityType = null,
    string? IdentityNumber = null,
    string? IdentityCountryCode = null);

public sealed record ScoutingCandidateListResponse(
    Guid Id,
    string Name,
    DateOnly? BirthDate,
    int Decision,
    int ObservationCount,
    DateOnly? LastObservedOn,
    double? AverageRating,
    string? LastObservedClub);

public sealed record ScoutingObservationResponse(
    Guid Id,
    DateOnly ObservedOn,
    int ObservationType,
    string? ObservedEvent,
    string? ObservedClub,
    string? ObservedTeam,
    int? DominantFoot,
    string? Strengths,
    string? Weaknesses,
    int? Recommendation,
    string? RecommendationNote,
    string ObserverName,
    int? Rating,
    IReadOnlyList<ScoutingPositionResponse> Positions);

public sealed record ScoutingPositionResponse(Guid Id, string Name);

public sealed record ScoutingCandidateDetailsResponse(
    Guid Id,
    string Name,
    DateOnly? BirthDate,
    string? PhoneNumber,
    string? Email,
    int SourceType,
    string? ReferrerName,
    string? SourceDescription,
    int Decision,
    IReadOnlyList<ScoutingObservationResponse> Observations,
    int? IdentityType = null,
    string? IdentityNumber = null,
    string? IdentityCountryCode = null,
    string? PhoneCountryCode = null,
    string? PhoneNationalNumber = null,
    Guid? RegisteredPersonId = null,
    Guid? RegisteredAtmacaCardId = null,
    string? RegisteredCardNumber = null);

public sealed record ScoutingRegistrationMatchResponse(
    Guid PersonId, Guid AtmacaCardId, string CardNumber, string FullName, bool IsExactIdentity);

public static class ScoutingLabels
{
    public static readonly (int Value, string Label)[] Sources =
        [(1, "Müsabaka"), (2, "Deneme"), (3, "Tavsiye"), (4, "Turnuva"), (5, "Okul müsabakası"), (6, "Antrenman"), (7, "Diğer")];
    public static readonly (int Value, string Label)[] ObservationTypes =
        [(1, "Müsabaka"), (2, "Deneme"), (3, "Turnuva"), (4, "Okul müsabakası"), (5, "Antrenman"), (6, "Diğer")];
    public static readonly (int Value, string Label)[] Feet = [(1, "Sağ"), (2, "Sol"), (3, "Her iki ayak")];
    public static readonly (int Value, string Label)[] Recommendations =
        [(1, "Kesinlikle öneriyorum"), (2, "Olumlu"), (3, "İzlemeye devam"), (4, "Yetersiz")];
    public static readonly (int Value, string Label)[] IdentityTypes =
        [(1, "T.C. Kimlik No"), (2, "Pasaport"), (3, "Ikamet izni"), (4, "Yabanci kimlik karti")];
    public static readonly (int Value, string Label)[] Decisions =
        [(1, "İzlemeye devam"), (2, "Olumlu"), (3, "Olumsuz")];

    public static string Of((int Value, string Label)[] set, int? value) =>
        value is null ? "—" : set.FirstOrDefault(x => x.Value == value).Label ?? "—";
}

public sealed class ScoutingObservationModel
{
    public DateOnly ObservedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int ObservationType { get; set; } = 1;
    public string? ObservedEvent { get; set; }
    public string? ObservedClub { get; set; }
    public string? ObservedTeam { get; set; }
    public int DominantFoot { get; set; }
    public string? Strengths { get; set; }
    public string? Weaknesses { get; set; }
    public int Recommendation { get; set; }
    public string? RecommendationNote { get; set; }
    public string ObserverName { get; set; } = "";
    public int Rating { get; set; }
    public HashSet<Guid> PositionIds { get; } = [];

    public static ScoutingObservationModel From(ScoutingObservationResponse o)
    {
        var model = new ScoutingObservationModel
        {
            ObservedOn = o.ObservedOn, ObservationType = o.ObservationType, ObservedEvent = o.ObservedEvent,
            ObservedClub = o.ObservedClub, ObservedTeam = o.ObservedTeam, DominantFoot = o.DominantFoot ?? 0,
            Strengths = o.Strengths, Weaknesses = o.Weaknesses, Recommendation = o.Recommendation ?? 0,
            RecommendationNote = o.RecommendationNote, ObserverName = o.ObserverName, Rating = o.Rating ?? 0
        };
        foreach (var p in o.Positions) model.PositionIds.Add(p.Id);
        return model;
    }

    public ScoutingObservationRequest ToRequest() => new(
        ObservedOn, ObservationType, ObservedEvent, ObservedClub, ObservedTeam,
        DominantFoot == 0 ? null : DominantFoot, Strengths, Weaknesses,
        Recommendation == 0 ? null : Recommendation, RecommendationNote, ObserverName,
        Rating == 0 ? null : Rating, PositionIds.ToList());
}
