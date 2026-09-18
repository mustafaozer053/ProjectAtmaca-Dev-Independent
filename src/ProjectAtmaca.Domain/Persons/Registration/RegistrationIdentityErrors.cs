using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons.Registration;

public static class RegistrationIdentityErrors
{
    public static readonly Error NationalIdentityNumberInvalidFormat = Error.Create(
        "RegistrationIdentity.NationalIdentityNumber.InvalidFormat", "National identity number must contain exactly eleven ASCII digits.");
    public static readonly Error StatusRequired = Error.Create(
        "RegistrationIdentity.Status.Required", "A supported Turkish citizenship status is required.");
    public static readonly Error NationalIdentityNumberRequired = Error.Create(
        "RegistrationIdentity.NationalIdentityNumber.Required", "Turkish national identity number is required.");
    public static readonly Error PassportNumberRequired = Error.Create(
        "RegistrationIdentity.PassportNumber.Required", "Passport number is required.");
    public static readonly Error AcquisitionDateRequired = Error.Create(
        "RegistrationIdentity.AcquisitionDate.Required", "Turkish citizenship acquisition date is required.");
    public static readonly Error AcquisitionDateNotApplicable = Error.Create(
        "RegistrationIdentity.AcquisitionDate.NotApplicable", "Turkish citizenship acquisition date applies only to acquired Turkish citizenship.");
}
