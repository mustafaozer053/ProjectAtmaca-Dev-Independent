using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Common.ValueObjects;

public sealed class SeasonName : ValueObject
{
    public string Value { get; }

    private SeasonName(string value)
    {
        Value = value;
    }

    public static Result<SeasonName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<SeasonName>.Failure(
                Error.Create(
                    "SEASON_NAME_REQUIRED",
                    "Season name is required."));
        }

        value = value.Trim();

        var parts = value.Split('-');

        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var startYear) ||
            !int.TryParse(parts[1], out var endYear))
        {
            return Result<SeasonName>.Failure(
                Error.Create(
                    "SEASON_NAME_INVALID_FORMAT",
                    "Season name must follow the 2026-2027 format."));
        }

        if (parts[0].Length != 4 || parts[1].Length != 4)
        {
            return Result<SeasonName>.Failure(
                Error.Create(
                    "SEASON_NAME_INVALID_FORMAT",
                    "Season years must contain four digits."));
        }

        if (endYear != startYear + 1)
        {
            return Result<SeasonName>.Failure(
                Error.Create(
                    "SEASON_NAME_INVALID_RANGE",
                    "Season end year must follow the start year."));
        }

        return Result<SeasonName>.Success(
            new SeasonName($"{startYear}-{endYear}"));
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
