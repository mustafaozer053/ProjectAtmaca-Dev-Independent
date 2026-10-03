using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;

namespace ProjectAtmaca.Web.Services;

public sealed class PeopleApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ProfessionalTitleResponse>> GetProfessionalTitlesAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var titles = await httpClient.GetFromJsonAsync<List<ProfessionalTitleResponse>>(
            $"api/persons/{personId:D}/professional-titles",
            cancellationToken);
        return titles ?? [];
    }

    public async Task AddProfessionalTitleAsync(
        Guid personId,
        string title,
        DateOnly startedOn,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/persons/{personId:D}/professional-titles",
            new { Title = title, StartedOn = startedOn },
            cancellationToken);
        await EnsureSuccessAsync(response, "Mesleki ünvan eklenemedi.", cancellationToken);
    }

    public async Task EndProfessionalTitleAsync(
        Guid personId,
        Guid titleId,
        DateOnly endedOn,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/persons/{personId:D}/professional-titles/{titleId:D}/end",
            new { EndedOn = endedOn },
            cancellationToken);
        await EnsureSuccessAsync(response, "Mesleki ünvan sonlandırılamadı.", cancellationToken);
    }

    public async Task UpdateProfessionalTitleAsync(
        Guid personId,
        Guid titleId,
        string title,
        DateOnly startedOn,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/persons/{personId:D}/professional-titles/{titleId:D}",
            new { Title = title, StartedOn = startedOn },
            cancellationToken);
        await EnsureSuccessAsync(response, "Mesleki ünvan güncellenemedi.", cancellationToken);
    }

    public async Task RemoveProfessionalTitleAsync(
        Guid personId,
        Guid titleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/persons/{personId:D}/professional-titles/{titleId:D}",
            cancellationToken);
        await EnsureSuccessAsync(response, "Mesleki ünvan silinemedi.", cancellationToken);
    }

    public async Task LinkProfessionalTitleDocumentAsync(
        Guid personId,
        Guid titleId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/persons/{personId:D}/professional-titles/{titleId:D}/documents/{documentId:D}",
            content: null,
            cancellationToken);
        await EnsureSuccessAsync(response, "Belge mesleki ünvana bağlanamadı.", cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        throw new HttpRequestException(
            !string.IsNullOrWhiteSpace(problem?.Detail)
                ? problem.Detail
                : !string.IsNullOrWhiteSpace(problem?.Title)
                    ? problem.Title
                    : $"{fallbackMessage} (HTTP {(int)response.StatusCode}).");
    }

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

    public async Task ChangeCardStatusAsync(
        Guid atmacaCardId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PatchAsJsonAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/status",
            new { IsActive = isActive },
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        var message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Kart durumu güncellenemedi (HTTP {(int)response.StatusCode}).";
        throw new HttpRequestException(message);
    }

    public async Task<IReadOnlyList<AtmacaCardSportsProfileResponse>> GetSportsProfilesAsync(
        Guid atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var profiles = await httpClient.GetFromJsonAsync<List<AtmacaCardSportsProfileResponse>>(
            $"api/atmaca-cards/{atmacaCardId:D}/sports-profiles",
            cancellationToken);
        return profiles ?? [];
    }

    public async Task UpsertSportsProfileAsync(
        Guid atmacaCardId,
        SportsProfileInput input,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/sports-profiles",
            input,
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        var message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Sporcu branş bilgileri kaydedilemedi (HTTP {(int)response.StatusCode}).";
        throw new HttpRequestException(message);
    }

    public async Task<IReadOnlyList<PositionCatalogItem>> GetPositionsAsync(
        string sportName,
        CancellationToken cancellationToken = default)
    {
        var positions = await httpClient.GetFromJsonAsync<List<PositionCatalogItem>>(
            $"api/positions?sportName={Uri.EscapeDataString(sportName)}&activeOnly=false",
            cancellationToken);
        return positions ?? [];
    }

    public async Task CreatePositionAsync(
        CreatePositionInput input,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/positions", input, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        var message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Mevki eklenemedi (HTTP {(int)response.StatusCode}).";
        throw new HttpRequestException(message);
    }

    public async Task ChangePositionStatusAsync(
        Guid positionId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PatchAsJsonAsync(
            $"api/positions/{positionId:D}/status",
            new { IsActive = isActive },
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        var message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Mevki durumu değiştirilemedi (HTTP {(int)response.StatusCode}).";
        throw new HttpRequestException(message);
    }

    public async Task<IReadOnlyList<AtmacaCardMeasurementResponse>> GetMeasurementsAsync(
        Guid atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var measurements = await httpClient.GetFromJsonAsync<List<AtmacaCardMeasurementResponse>>(
            $"api/atmaca-cards/{atmacaCardId:D}/measurements",
            cancellationToken);
        return measurements ?? [];
    }

    public async Task RecordMeasurementAsync(
        Guid atmacaCardId,
        MeasurementInput input,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/measurements",
            input,
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        var message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Ölçüm kaydedilemedi (HTTP {(int)response.StatusCode}).";
        throw new HttpRequestException(message);
    }

    public async Task<AtmacaCardEducationResponse> GetEducationAsync(
        Guid atmacaCardId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<AtmacaCardEducationResponse>(
            $"api/atmaca-cards/{atmacaCardId:D}/education",
            cancellationToken)
        ?? throw new InvalidOperationException("Atmaca Card education response was empty.");

    public async Task UpdateEducationAsync(
        Guid atmacaCardId,
        EducationInput input,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/education",
            input,
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        var message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Eğitim bilgileri kaydedilemedi (HTTP {(int)response.StatusCode}).";
        throw new HttpRequestException(message);
    }

    public async Task<IReadOnlyList<AtmacaCardDocumentResponse>> GetDocumentsAsync(
        Guid atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        var documents = await httpClient.GetFromJsonAsync<List<AtmacaCardDocumentResponse>>(
            $"api/atmaca-cards/{atmacaCardId:D}/documents",
            cancellationToken);
        return documents ?? [];
    }

    public async Task UploadDocumentAsync(
        Guid atmacaCardId,
        DocumentInput input,
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(input.DocumentType.ToString()), nameof(input.DocumentType));
        form.Add(new StringContent(input.Title), nameof(input.Title));
        if (!string.IsNullOrWhiteSpace(input.Issuer))
            form.Add(new StringContent(input.Issuer), nameof(input.Issuer));
        if (input.IssuedOn.HasValue)
            form.Add(new StringContent(input.IssuedOn.Value.ToString("yyyy-MM-dd")), nameof(input.IssuedOn));

        await using var fileStream = file.OpenReadStream(10 * 1024 * 1024, cancellationToken);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        form.Add(streamContent, "File", Path.GetFileName(file.Name));

        using var response = await httpClient.PostAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/documents",
            form,
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        var message = !string.IsNullOrWhiteSpace(problem?.Detail)
            ? problem.Detail
            : !string.IsNullOrWhiteSpace(problem?.Title)
                ? problem.Title
                : $"Belge kaydedilemedi (HTTP {(int)response.StatusCode}).";
        throw new HttpRequestException(message);
    }

    public async Task<DocumentDownload> DownloadDocumentAsync(
        Guid atmacaCardId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/documents/{documentId:D}/file",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
                cancellationToken: cancellationToken);
            throw new HttpRequestException(
                !string.IsNullOrWhiteSpace(problem?.Detail)
                    ? problem.Detail
                    : $"Belge indirilemedi (HTTP {(int)response.StatusCode}).");
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "belge";
        return new DocumentDownload(
            await response.Content.ReadAsByteArrayAsync(cancellationToken),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            fileName.Trim('"'));
    }

    public async Task SavePhotoAsync(
        Guid atmacaCardId,
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        await using var fileStream = file.OpenReadStream(5 * 1024 * 1024, cancellationToken);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        form.Add(streamContent, "File", Path.GetFileName(file.Name));

        using var response = await httpClient.PutAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/photo",
            form,
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
            cancellationToken: cancellationToken);
        throw new HttpRequestException(
            !string.IsNullOrWhiteSpace(problem?.Detail)
                ? problem.Detail
                : $"Fotoğraf kaydedilemedi (HTTP {(int)response.StatusCode}).");
    }

    public async Task<string?> GetPhotoDataUriAsync(
        Guid atmacaCardId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/atmaca-cards/{atmacaCardId:D}/photo",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<CardStatusProblem>(
                cancellationToken: cancellationToken);
            throw new HttpRequestException(
                !string.IsNullOrWhiteSpace(problem?.Detail)
                    ? problem.Detail
                    : $"Fotoğraf yüklenemedi (HTTP {(int)response.StatusCode}).");
        }

        string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return $"data:{contentType};base64,{Convert.ToBase64String(content)}";
    }

    public sealed record SportsProfileInput(
        string SportName,
        string? LicenseNumber,
        DateOnly? StartedSportOn,
        DateOnly? ClubRegisteredOn,
        int? CompetitionLevel,
        bool? IsNationalAthlete,
        IReadOnlyCollection<Guid>? PositionIds = null);

    public sealed record CreatePositionInput(string SportName, string Code, string Name);

    public sealed record EducationInput(
        bool IsCurrentlyStudying,
        string? SchoolName,
        string? SchoolGrade,
        string? SchoolNumber);

    public sealed record DocumentInput(
        int DocumentType,
        string Title,
        string? Issuer,
        DateOnly? IssuedOn);

    public sealed record DocumentDownload(byte[] Content, string ContentType, string FileName);

    public sealed record MeasurementInput(
        DateOnly MeasuredOn,
        decimal? HeightCentimeters,
        decimal? WeightKilograms);

    private sealed class CardStatusProblem
    {
        public string? Title { get; init; }
        public string? Detail { get; init; }
    }
}
