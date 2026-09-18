using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Clubs;

public sealed class Academy : AuditableAggregateRoot
{
    public Guid ClubId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string? AgeGroup { get; private set; }

    public bool IsActive { get; private set; }

    private Academy(
        Guid id,
        Guid clubId,
        string name,
        string? description,
        string? ageGroup)
        : base(id)
    {
        ClubId = clubId;
        Name = name;
        Description = description;
        AgeGroup = ageGroup;
        IsActive = true;
    }

    public static Result<Academy> Create(
        Guid clubId,
        string name,
        string? description = null,
        string? ageGroup = null)
    {
        if (clubId == Guid.Empty)
        {
            return Result<Academy>.Failure(
                Error.Create(
                    "ACADEMY_CLUB_ID_REQUIRED",
                    "Club id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Academy>.Failure(
                Error.Create(
                    "ACADEMY_NAME_REQUIRED",
                    "Academy name is required."));
        }

        return Result<Academy>.Success(
            new Academy(
                Guid.NewGuid(),
                clubId,
                name.Trim(),
                NormalizeOptionalText(description),
                NormalizeOptionalText(ageGroup)));
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Create(
                    "ACADEMY_NAME_REQUIRED",
                    "Academy name is required."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void UpdateDescription(string? description)
    {
        Description = NormalizeOptionalText(description);
    }

    public void UpdateAgeGroup(string? ageGroup)
    {
        AgeGroup = NormalizeOptionalText(ageGroup);
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
