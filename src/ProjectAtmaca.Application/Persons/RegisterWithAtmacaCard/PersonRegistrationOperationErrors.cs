using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

public static class PersonRegistrationOperationErrors
{
    public static readonly Error CitizenshipIdentityConflict = Error.Create(
        "PersonRegistration.Citizenship.IdentityConflict", "Supplementary Turkish citizenship contradicts the selected registration identity.");
    public static readonly Error DuplicateCitizenship = Error.Create("PersonRegistration.Citizenship.DuplicateCountry", "Each country may appear only once in initial registration.");
    public static readonly Error PassportDuplicateReasonRequired = Error.Create(
        "PersonRegistration.Passport.DuplicateReasonRequired", "A reason is required to confirm a possible passport duplicate.");
    public static readonly Error PassportDuplicateConfirmationRequired = Error.Create(
        "PersonRegistration.Passport.PossibleDuplicate", "A matching passport number exists; explicit confirmation is required to register a different person.");
    public static readonly Error IdentityAlreadyRegistered = Error.Create("PersonRegistration.Identity.AlreadyRegistered", "The identity number is already registered.");
    public static readonly Error CardNumberCapacity = Error.Create("PersonRegistration.CardNumber.Capacity", "Card number capacity has been exhausted.");
    public static readonly Error OperationRequired = Error.Create("PersonRegistration.Operation.Required", "Operation id is required.");
    public static readonly Error OperationConflict = Error.Create("PersonRegistration.Operation.Conflict", "Operation input differs from the completed registration.");
}
