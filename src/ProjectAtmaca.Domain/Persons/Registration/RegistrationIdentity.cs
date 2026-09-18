using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons.Registration;

/// <summary>Required identity fields at registration; does not verify document authenticity.</summary>
public sealed class RegistrationIdentity
{
    public TurkishCitizenshipStatus Status { get; }
    public string? NationalIdentityNumber { get; }
    public string? PassportNumber { get; }
    public DateOnly? TurkishCitizenshipAcquiredOn { get; }

    private RegistrationIdentity(TurkishCitizenshipStatus status, string? nationalIdentityNumber,
        string? passportNumber, DateOnly? turkishCitizenshipAcquiredOn)
    {
        Status = status;
        NationalIdentityNumber = nationalIdentityNumber;
        PassportNumber = passportNumber;
        TurkishCitizenshipAcquiredOn = turkishCitizenshipAcquiredOn;
    }

    public static Result<RegistrationIdentity> Create(
        TurkishCitizenshipStatus status,
        string? nationalIdentityNumber = null,
        string? passportNumber = null,
        DateOnly? turkishCitizenshipAcquiredOn = null)
    {
        if (!Enum.IsDefined(status))
            return Result<RegistrationIdentity>.Failure(RegistrationIdentityErrors.StatusRequired);

        if (status != TurkishCitizenshipStatus.NotTurkishCitizen && string.IsNullOrWhiteSpace(nationalIdentityNumber))
            return Result<RegistrationIdentity>.Failure(RegistrationIdentityErrors.NationalIdentityNumberRequired);

        var normalizedNationalId = Normalize(nationalIdentityNumber);
        if (normalizedNationalId is not null && (normalizedNationalId.Length != 11 ||
            normalizedNationalId.Any(character => character < '0' || character > '9')))
            return Result<RegistrationIdentity>.Failure(RegistrationIdentityErrors.NationalIdentityNumberInvalidFormat);

        if (status == TurkishCitizenshipStatus.NotTurkishCitizen && string.IsNullOrWhiteSpace(passportNumber))
            return Result<RegistrationIdentity>.Failure(RegistrationIdentityErrors.PassportNumberRequired);

        if (status == TurkishCitizenshipStatus.Acquired &&
            (turkishCitizenshipAcquiredOn is null || turkishCitizenshipAcquiredOn == DateOnly.MinValue))
            return Result<RegistrationIdentity>.Failure(RegistrationIdentityErrors.AcquisitionDateRequired);

        if (status != TurkishCitizenshipStatus.Acquired && turkishCitizenshipAcquiredOn is not null)
            return Result<RegistrationIdentity>.Failure(RegistrationIdentityErrors.AcquisitionDateNotApplicable);

        return Result<RegistrationIdentity>.Success(new RegistrationIdentity(
            status, Normalize(nationalIdentityNumber), Normalize(passportNumber), turkishCitizenshipAcquiredOn));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
