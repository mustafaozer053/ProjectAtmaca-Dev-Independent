using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Clubs;

public sealed class Branch : AuditableAggregateRoot
{
    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    private Branch(
        Guid id,
        string name,
        string? description)
        : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public static Result<Branch> Create(
        string name,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Branch>.Failure(
                Error.Create(
                    "BRANCH_NAME_REQUIRED",
                    "Branch name is required."));
        }

        return Result<Branch>.Success(
            new Branch(
                Guid.NewGuid(),
                name.Trim(),
                NormalizeOptionalText(description)));
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Create(
                    "BRANCH_NAME_REQUIRED",
                    "Branch name is required."));
        }

        Name = name.Trim();
        return Result.Success();
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
