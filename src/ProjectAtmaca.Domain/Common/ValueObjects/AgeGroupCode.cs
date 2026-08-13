using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class AgeGroupCode : ValueObject
{
    public string Value { get; }

    private AgeGroupCode(string value)
    {
        Value = value;
    }

    public static Result<AgeGroupCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<AgeGroupCode>.Failure(
                Error.Create(
                    "AGE_GROUP_CODE_REQUIRED",
                    "Age group code is required."));
        }

        var normalizedValue = value.Trim().ToUpperInvariant();

        if (!normalizedValue.StartsWith('U'))
        {
            return Result<AgeGroupCode>.Failure(
                Error.Create(
                    "AGE_GROUP_CODE_INVALID_PREFIX",
                    "Age group code must start with U."));
        }

        if (!int.TryParse(normalizedValue[1..], out var age))
        {
            return Result<AgeGroupCode>.Failure(
                Error.Create(
                    "AGE_GROUP_CODE_INVALID_FORMAT",
                    "Age group code must follow the U15 format."));
        }

        if (age <= 0)
        {
            return Result<AgeGroupCode>.Failure(
                Error.Create(
                    "AGE_GROUP_CODE_INVALID_AGE",
                    "Age group value must be greater than zero."));
        }

        return Result<AgeGroupCode>.Success(
            new AgeGroupCode(normalizedValue));
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
