using System.Net.Http.Json;

namespace ProjectAtmaca.Web.Services;

public sealed class TrainingsApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<TrainingListItemResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<TrainingListItemResponse>>(
            "api/trainings", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<TrainingTypeResponse>> ListTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<TrainingTypeResponse>>(
            "api/training-types", cancellationToken);
        return result ?? [];
    }

    public async Task<TrainingDetailsResponse?> GetByIdAsync(
        Guid trainingId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TrainingDetailsResponse>(
            $"api/trainings/{trainingId:D}",
            cancellationToken);
    }

    public async Task<Guid?> CreateAsync(
        Guid seasonId,
        Guid organizationId,
        Guid seasonTeamId,
        string title,
        string location,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid trainingTypeId,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/trainings",
            new
            {
                SeasonId = seasonId,
                OrganizationId = organizationId,
                SeasonTeamId = seasonTeamId,
                Title = title,
                Description = (string?)null,
                Location = location,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                Assignments = new[]
                {
                    new { TrainingTypeId = trainingTypeId, DurationMinutes = durationMinutes }
                }
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<CreateTrainingResponse>(
            cancellationToken: cancellationToken);
        return result?.Id;
    }

    public async Task<bool> ChangeStatusAsync(
        Guid trainingId,
        string action,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/trainings/{trainingId:D}/{action}",
            content: null,
            cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<ParticipationListItemResponse>> ListParticipationsAsync(
        Guid trainingId,
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<ParticipationListItemResponse>>(
            $"api/participations?activityTypeCode=TRAINING&activityId={trainingId:D}",
            cancellationToken);
        return result ?? [];
    }

    public async Task<ParticipationSummaryResponse?> GetParticipationSummaryAsync(
        Guid trainingId,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ParticipationSummaryResponse>(
            $"api/participations/summary?activityTypeCode=TRAINING&activityId={trainingId:D}",
            cancellationToken);
    }

    public async Task AddParticipationAsync(
        Guid trainingId,
        Guid atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/trainings/{trainingId:D}/participations",
            new { AtmacaCardId = atmacaCardId },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetParticipationStatusAsync(
        Guid participationId,
        bool present,
        CancellationToken cancellationToken = default)
    {
        using var response = present
            ? await httpClient.PostAsJsonAsync(
                $"api/participations/{participationId:D}/mark-present",
                new { ConditionCode = (string?)null },
                cancellationToken)
            : await httpClient.PostAsync(
                $"api/participations/{participationId:D}/mark-absent",
                content: null,
                cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record CreateTrainingResponse(Guid Id);
}

public sealed record ParticipationListItemResponse(
    Guid Id,
    Guid AtmacaCardId,
    string Status,
    string? ConditionCode,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LeftAt);

public sealed record ParticipationSummaryResponse(
    int Total,
    int NotRecorded,
    int Present,
    int Absent);
