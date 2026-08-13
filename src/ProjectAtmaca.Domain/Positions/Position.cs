using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Positions;

public sealed class Position : AuditableAggregateRoot
{
    public string Code { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    private Position()
    {
        Code = null!;
        Name = null!;
    }

    private Position(
        Guid id,
        string code,
        string name)
        : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }

    public static Result<Position> Create(
        string code,
        string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<Position>.Failure(
                Error.Create(
                    "POSITION_CODE_REQUIRED",
                    "Position code is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Position>.Failure(
                Error.Create(
                    "POSITION_NAME_REQUIRED",
                    "Position name is required."));
        }

        var normalizedCode = code
            .Trim()
            .ToUpperInvariant();

        var normalizedName = name.Trim();

        return Result<Position>.Success(
            new Position(
                Guid.NewGuid(),
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
