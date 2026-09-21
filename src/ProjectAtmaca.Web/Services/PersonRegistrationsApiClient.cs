using System.Net.Http.Json;

namespace ProjectAtmaca.Web.Services;

public sealed record PersonRegistrationOutcome(
    bool IsSuccess,
    RegistrationReceiptResponse? Receipt,
    string? ErrorCode,
    string? ErrorDetail);

// Thin typed HttpClient wrapper around POST /api/person-registrations.
public sealed class PersonRegistrationsApiClient
{
    private readonly HttpClient _httpClient;

    public PersonRegistrationsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PersonRegistrationOutcome> RegisterAsync(
        RegisterPersonRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/person-registrations",
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var receipt = await response.Content.ReadFromJsonAsync<
                RegistrationReceiptResponse>(cancellationToken: cancellationToken);

            return new PersonRegistrationOutcome(true, receipt, null, null);
        }

        var problem = await response.Content.ReadFromJsonAsync<
            PersonRegistrationProblemResponse>(cancellationToken: cancellationToken);

        return new PersonRegistrationOutcome(
            false,
            null,
            problem?.Code,
            problem?.Detail ?? "Kayıt sırasında bilinmeyen bir hata oluştu.");
    }
}
