using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Clubs;

public sealed class Team : AuditableAggregateRoot
{
    public Guid ClubId { get; private set; }

    public Guid AcademyId { get; private set; }

    public string Name { get; private set; }

    public string? AgeGroup { get; private set; }

    public string? Category { get; private set; }

    public bool IsActive { get; private set; }

    private Team(
        Guid id,
        Guid clubId,
        Guid academyId,
        string name,
        string? ageGroup,
        string? category)
        : base(id)
    {
        ClubId = clubId;
        AcademyId = academyId;
        Name = name;
        AgeGroup = ageGroup;
        Category = category;
        IsActive = true;
    }

    public static Result<Team> Create(
        Guid clubId,
        Guid academyId,
        string name,
        string? ageGroup = null,
        string? category = null)
    {
        if (clubId == Guid.Empty)
        {
            return Result<Team>.Failure(
                Error.Create(
                    "TEAM_CLUB_ID_REQUIRED",
                    "Club id is required."));
        }

        if (academyId == Guid.Empty)
        {
            return Result<Team>.Failure(
                Error.Create(
                    "TEAM_ACADEMY_ID_REQUIRED",
                    "Academy id is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Team>.Failure(
                Error.Create(
                    "TEAM_NAME_REQUIRED",
                    "Team name is required."));
        }

        return Result<Team>.Success(
            new Team(
                Guid.NewGuid(),
                clubId,
                academyId,
                name.Trim(),
                NormalizeOptionalText(ageGroup),
                NormalizeOptionalText(category)));
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Create(
                    "TEAM_NAME_REQUIRED",
                    "Team name is required."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void UpdateAgeGroup(string? ageGroup)
    {
        AgeGroup = NormalizeOptionalText(ageGroup);
    }

    public void UpdateCategory(string? category)
    {
        Category = NormalizeOptionalText(category);
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
