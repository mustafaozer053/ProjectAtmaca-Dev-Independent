using System.Net.Http.Json;

namespace ProjectAtmaca.Web.Services;

public sealed class OrganizationApiClient(HttpClient httpClient)
{
    public Task<List<OrganizationManagementItem>?> GetOrganizationsAsync(
        CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<List<OrganizationManagementItem>>(
            "api/organizations",
            cancellationToken);

    public async Task<OrganizationApiResult> CreateAsync(
        CreateOrganizationInput input,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/organizations",
            input,
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return new OrganizationApiResult(true, null);

        var problem = await response.Content.ReadFromJsonAsync<OrganizationApiProblem>(
            cancellationToken: cancellationToken);
        string message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Kurum kaydedilemedi (HTTP {(int)response.StatusCode}).";
        return new OrganizationApiResult(false, message);
    }

    public async Task<OrganizationApiResult> UpdateAsync(
        Guid organizationId,
        CreateOrganizationInput input,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/organizations/{organizationId:D}",
            input,
            cancellationToken);
        return await ToResultAsync(response, "Kurum güncellenemedi.", cancellationToken);
    }

    public async Task<OrganizationApiResult> ChangeStatusAsync(
        Guid organizationId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PatchAsJsonAsync(
            $"api/organizations/{organizationId:D}/status",
            new { IsActive = isActive },
            cancellationToken);
        return await ToResultAsync(response, "Kurum durumu değiştirilemedi.", cancellationToken);
    }

    private static async Task<OrganizationApiResult> ToResultAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return new OrganizationApiResult(true, null);

        var problem = await response.Content.ReadFromJsonAsync<OrganizationApiProblem>(
            cancellationToken: cancellationToken);
        string message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"{fallback} (HTTP {(int)response.StatusCode}).";
        return new OrganizationApiResult(false, message);
    }
}

public sealed record OrganizationManagementItem(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid? ParentOrganizationId,
    bool IsActive,
    string HierarchyPath);

public sealed record CreateOrganizationInput(
    string Name,
    string Code,
    string? Description,
    Guid? ParentOrganizationId);

public sealed record OrganizationApiResult(bool Succeeded, string? ErrorMessage);

internal sealed class OrganizationApiProblem
{
    public string? Title { get; init; }
    public string? Detail { get; init; }
}
