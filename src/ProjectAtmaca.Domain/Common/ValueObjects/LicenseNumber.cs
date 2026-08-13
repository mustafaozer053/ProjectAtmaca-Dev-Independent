using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class LicenseNumber : ValueObject
{
    public string Value { get; }

    private LicenseNumber(string value)
    {
        Value = value;
    }

    public static Result<LicenseNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<LicenseNumber>.Failure(
                Error.Create(
                    "LICENSE_NUMBER_REQUIRED",
                    "License number is required."));
        }

        var normalizedValue = value.Trim();

        return Result<LicenseNumber>.Success(
            new LicenseNumber(normalizedValue));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
