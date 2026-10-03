using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Positions;

public sealed class Position : AuditableAggregateRoot
{
    public string SportName { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    private Position()
    {
        SportName = null!;
        Code = null!;
        Name = null!;
    }

    private Position(
        Guid id,
        string sportName,
        string code,
        string name)
        : base(id)
    {
        SportName = sportName;
        Code = code;
        Name = name;
        IsActive = true;
    }

    public static Result<Position> Create(
        string sportName,
        string code,
        string name)
    {
        if (string.IsNullOrWhiteSpace(sportName) || sportName.Trim().Length > 80)
        {
            return Result<Position>.Failure(
                Error.Create(
                    "POSITION_SPORT_REQUIRED",
                    "Position sport is required and must not exceed 80 characters."));
        }

        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 30)
        {
            return Result<Position>.Failure(
                Error.Create(
                    "POSITION_CODE_REQUIRED",
                    "Position code is required and must not exceed 30 characters."));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            return Result<Position>.Failure(
                Error.Create(
                    "POSITION_NAME_REQUIRED",
                    "Position name is required and must not exceed 100 characters."));
        }

        var normalizedCode = code
            .Trim()
            .ToUpperInvariant();

        var normalizedName = name.Trim();

        return Result<Position>.Success(
            new Position(
                Guid.NewGuid(),
                sportName.Trim(),
                normalizedCode,
                normalizedName));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
