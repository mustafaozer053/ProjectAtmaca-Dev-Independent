namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

// Input is sensitive historical data; never log or return this object through transport.
public sealed class CompletedPersonRegistration
{
    public PersonRegistrationInput Input { get; }
    public RegistrationReceipt Receipt { get; }

    public CompletedPersonRegistration(PersonRegistrationInput input, RegistrationReceipt receipt)
    {
        Input = Normalize(input ?? throw new ArgumentNullException(nameof(input)));
        Receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
    }

    public bool Matches(PersonRegistrationInput input)
    {
        var normalized = Normalize(input);
        // Ignore collection reference/order; compare every entry by country identity and date.
        return (Input with { Citizenships = Array.Empty<CitizenshipInput>() }) ==
            (normalized with { Citizenships = Array.Empty<CitizenshipInput>() }) &&
            Input.Citizenships.OrderBy(x => x?.Country?.Code, StringComparer.Ordinal).ThenBy(x => x?.AcquiredOn)
                .SequenceEqual(normalized.Citizenships.OrderBy(x => x?.Country?.Code, StringComparer.Ordinal).ThenBy(x => x?.AcquiredOn));
    }

    internal static PersonRegistrationInput Normalize(PersonRegistrationInput input) => input with
    {
        NationalIdentityNumber = NormalizeNumber(input.NationalIdentityNumber),
        PassportNumber = NormalizeNumber(input.PassportNumber),
        PassportDuplicateReason = NormalizeNumber(input.PassportDuplicateReason)
    };

    private static string? NormalizeNumber(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
