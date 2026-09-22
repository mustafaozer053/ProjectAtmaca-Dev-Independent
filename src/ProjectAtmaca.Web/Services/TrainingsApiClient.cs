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

    private sealed record CreateTrainingResponse(Guid Id);
}
