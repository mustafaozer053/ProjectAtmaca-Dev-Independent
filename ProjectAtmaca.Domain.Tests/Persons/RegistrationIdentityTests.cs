using FluentAssertions;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class RegistrationIdentityTests
{
    [Theory]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("1234567890A")]
    [InlineData("１２３４５６７８９０１")]
    public void Create_Should_RejectAmbiguousNationalIdentityFormat(string value)
    {
        RegistrationIdentity.Create(TurkishCitizenshipStatus.ByBirth, value).Error
            .Should().BeSameAs(RegistrationIdentityErrors.NationalIdentityNumberInvalidFormat);
    }
    [Theory]
    [InlineData(TurkishCitizenshipStatus.ByBirth, "12345678901", null)]
    [InlineData(TurkishCitizenshipStatus.NotTurkishCitizen, null, "AB12345")]
    public void Create_Should_AcceptMinimumFields_WithoutCitizenshipCountriesOrDate(
        TurkishCitizenshipStatus status, string? nationalId, string? passport)
    {
        var result = RegistrationIdentity.Create(status, nationalId, passport);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(status);
        result.Value.NationalIdentityNumber.Should().Be(nationalId);
        result.Value.PassportNumber.Should().Be(passport);
        result.Value.TurkishCitizenshipAcquiredOn.Should().BeNull();
    }

    [Fact]
    public void Create_Should_PreserveAcquisitionDateAndOptionalPassport_ForNaturalizedCitizen()
    {
        var date = new DateOnly(2020, 6, 12);
        var result = RegistrationIdentity.Create(TurkishCitizenshipStatus.Acquired,
            " 12345678901 ", " AB12345 ", date);
        result.IsSuccess.Should().BeTrue();
        result.Value!.NationalIdentityNumber.Should().Be("12345678901");
        result.Value.PassportNumber.Should().Be("AB12345");
        result.Value.TurkishCitizenshipAcquiredOn.Should().Be(date);
    }

    [Theory]
    [InlineData(TurkishCitizenshipStatus.ByBirth, null)]
    [InlineData(TurkishCitizenshipStatus.ByBirth, "")]
    [InlineData(TurkishCitizenshipStatus.ByBirth, "   ")]
    [InlineData(TurkishCitizenshipStatus.Acquired, null)]
    [InlineData(TurkishCitizenshipStatus.Acquired, "")]
    [InlineData(TurkishCitizenshipStatus.Acquired, "   ")]
    public void Create_Should_RejectMissingNationalId_EvenWhenPassportExists(
        TurkishCitizenshipStatus status, string? nationalId)
    {
        var result = RegistrationIdentity.Create(status, nationalId, "AB12345", new DateOnly(2020, 6, 12));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(RegistrationIdentityErrors.NationalIdentityNumberRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_RejectMissingPassport_ForNonTurkishCitizen(string? passport)
    {
        var result = RegistrationIdentity.Create(TurkishCitizenshipStatus.NotTurkishCitizen,
            nationalIdentityNumber: "12345678901", passportNumber: passport);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(RegistrationIdentityErrors.PassportNumberRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public void Create_Should_RejectMissingOrUnsupportedSelection(int status)
    {
        var result = RegistrationIdentity.Create((TurkishCitizenshipStatus)status, "12345678901", "AB12345");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(RegistrationIdentityErrors.StatusRequired);
    }

    [Theory]
    [InlineData(TurkishCitizenshipStatus.ByBirth)]
    [InlineData(TurkishCitizenshipStatus.NotTurkishCitizen)]
    public void Create_Should_RejectAcquisitionDate_ForIncompatibleSelection(TurkishCitizenshipStatus status)
    {
        var result = RegistrationIdentity.Create(status, "12345678901", "AB12345", new DateOnly(2020, 6, 12));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(RegistrationIdentityErrors.AcquisitionDateNotApplicable);
    }

    [Fact]
    public void Create_Should_RejectDefaultAcquisitionDate_ForNaturalizedCitizen()
    {
        var result = RegistrationIdentity.Create(TurkishCitizenshipStatus.Acquired,
            nationalIdentityNumber: "12345678901", turkishCitizenshipAcquiredOn: default(DateOnly));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(RegistrationIdentityErrors.AcquisitionDateRequired);
    }

    [Fact]
    public void Create_Should_RequireAcquisitionDate_ForNaturalizedTurkishCitizen()
    {
        var result = RegistrationIdentity.Create(
            TurkishCitizenshipStatus.Acquired,
            nationalIdentityNumber: "12345678901");

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeSameAs(RegistrationIdentityErrors.AcquisitionDateRequired);
    }
}
