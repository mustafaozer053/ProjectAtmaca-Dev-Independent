using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;
using ProjectAtmaca.Domain.Persons.Registration;
using ProjectAtmaca.Domain.Services;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

/// <summary>
/// Prepares a new registration after authorization. Does not save records or implement replay.
/// A future transaction coordinator must resolve replay and person matching before invoking this step.
/// </summary>
public sealed class PreparePersonRegistration
{
    private readonly PersonRegistrationAuthorization _authorization;
    private readonly IAtmacaCardNumberGenerator _numbers;
    private readonly TimeProvider _clock;

    public PreparePersonRegistration(PersonRegistrationAuthorization authorization,
        IAtmacaCardNumberGenerator numbers, TimeProvider clock)
    {
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<Result<PreparedPersonRegistration>> PrepareAsync(PersonRegistrationInput input,
        CancellationToken cancellationToken = default) =>
        _authorization.ExecuteAsync(token => PrepareAuthorizedAsync(input, token), cancellationToken);

    // Assembly-internal: the coordinator authorizes before replay lookup; do not authorize twice.
    internal async Task<Result<PreparedPersonRegistration>> PrepareAuthorizedAsync(
        PersonRegistrationInput input, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(input);
            var identity = RegistrationIdentity.Create(input.Status, input.NationalIdentityNumber,
                input.PassportNumber, input.TurkishCitizenshipAcquiredOn);
            if (identity.IsFailure)
                return Result<PreparedPersonRegistration>.Failure(identity.Error!);

            var person = Person.Create(input.Name, input.BirthDate, input.BirthCountry,
                input.BirthPlace, input.MotherName, input.FatherName, input.BloodType, input.Email,
                input.PrimaryPhoneNumber, input.SecondaryPhoneNumber, input.Address);
            if (person.IsFailure)
                return Result<PreparedPersonRegistration>.Failure(person.Error!);

            var registration = PersonRegistration.Record(person.Value!.Id, identity.Value!);
            if (registration.IsFailure)
                return Result<PreparedPersonRegistration>.Failure(registration.Error!);

            var citizenships = new List<PersonCitizenship>();
            var countries = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in input.Citizenships)
            {
                var citizenship = PersonCitizenship.Register(person.Value.Id, entry?.Country!, entry?.AcquiredOn);
                if (citizenship.IsFailure)
                    return Result<PreparedPersonRegistration>.Failure(citizenship.Error!);
                if (citizenship.Value!.Country.Code == "TR" &&
                    (identity.Value!.Status == TurkishCitizenshipStatus.NotTurkishCitizen ||
                     (identity.Value.Status == TurkishCitizenshipStatus.Acquired &&
                      citizenship.Value.AcquiredOn is not null &&
                      citizenship.Value.AcquiredOn != identity.Value.TurkishCitizenshipAcquiredOn)))
                    return Result<PreparedPersonRegistration>.Failure(PersonRegistrationOperationErrors.CitizenshipIdentityConflict);
                if (!countries.Add(citizenship.Value!.Country.Code))
                    return Result<PreparedPersonRegistration>.Failure(PersonRegistrationOperationErrors.DuplicateCitizenship);
                citizenships.Add(citizenship.Value);
            }

            token.ThrowIfCancellationRequested();
            string generated;
            try
            {
                generated = await _numbers.GenerateAsync(token);
            }
            catch (AtmacaCardNumberCapacityException)
            {
                return Result<PreparedPersonRegistration>.Failure(PersonRegistrationOperationErrors.CardNumberCapacity);
            }
            var number = AtmacaCardNumber.Create(generated);
            if (number.IsFailure)
                return Result<PreparedPersonRegistration>.Failure(number.Error!);

            token.ThrowIfCancellationRequested();
            var card = AtmacaCard.Issue(person.Value.Id, number.Value!, _clock.GetUtcNow().UtcDateTime);
            if (card.IsFailure)
                return Result<PreparedPersonRegistration>.Failure(card.Error!);

            return Result<PreparedPersonRegistration>.Success(
                new PreparedPersonRegistration(person.Value, registration.Value!, card.Value!, citizenships));
        }
}
