using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Clubs;

public sealed class Club : AuditableAggregateRoot
{
    public string Name { get; private set; }

    public string? ShortName { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    private Club(
        Guid id,
        string name,
        string? shortName,
        string? description)
        : base(id)
    {
        Name = name;
        ShortName = shortName;
        Description = description;
        IsActive = true;
    }

    public static Result<Club> Create(
        string name,
        string? shortName = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Club>.Failure(
                Error.Create(
                    "CLUB_NAME_REQUIRED",
                    "Club name is required."));
        }

        var normalizedName = name.Trim();
        var normalizedShortName = NormalizeOptionalText(shortName);
        var normalizedDescription = NormalizeOptionalText(description);

        return Result<Club>.Success(
            new Club(
                Guid.NewGuid(),
                normalizedName,
                normalizedShortName,
                normalizedDescription));
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Create(
                    "CLUB_NAME_REQUIRED",
                    "Club name is required."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void UpdateShortName(string? shortName)
    {
        ShortName = NormalizeOptionalText(shortName);
    }

    public void UpdateDescription(string? description)
    {
        Description = NormalizeOptionalText(description);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
