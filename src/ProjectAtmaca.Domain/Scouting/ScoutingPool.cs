using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Scouting.Entities;

namespace ProjectAtmaca.Domain.Scouting;

public sealed class ScoutingPool : AuditableAggregateRoot
{
    private readonly List<ScoutingPoolMembership> _memberships = [];

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ScoutingPoolMembership> Memberships =>
        _memberships.AsReadOnly();

    private ScoutingPool()
    {
        Name = null!;
    }

    private ScoutingPool(
        Guid id,
        string name,
        string? description)
        : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public static Result<ScoutingPool> Create(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ScoutingPool>.Failure(
                Error.Create(
                    "SCOUTING_POOL_NAME_REQUIRED",
                    "Scouting pool name is required."));
        }

        var normalizedName = name.Trim();
        var normalizedDescription =
            NormalizeOptionalText(description);

        return Result<ScoutingPool>.Success(
            new ScoutingPool(
                Guid.NewGuid(),
                normalizedName,
                normalizedDescription));
    }

    public Result AddCandidate(
        Guid scoutingCandidateId,
        Guid addedByAssignmentId,
        string? note)
    {
        if (!IsActive)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_POOL_INACTIVE",
                    "Candidates cannot be added to an inactive scouting pool."));
        }

        var hasActiveMembership = _memberships.Any(
            x => x.ScoutingCandidateId == scoutingCandidateId &&
                 x.IsActive);

        if (hasActiveMembership)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_POOL_MEMBERSHIP_DUPLICATE",
                    "The scouting candidate already has an active membership in this scouting pool."));
        }

        var membershipResult =
            ScoutingPoolMembership.Create(
                scoutingCandidateId,
                addedByAssignmentId,
                note);

        if (membershipResult.IsFailure)
        {
            return Result.Failure(
                membershipResult.Error!);
        }

        _memberships.Add(membershipResult.Value!);

        return Result.Success();
    }

    public Result RemoveCandidate(
        Guid scoutingCandidateId,
        Guid removedByAssignmentId,
        string? removalNote)
    {
        var membership = _memberships.FirstOrDefault(
            x => x.ScoutingCandidateId == scoutingCandidateId &&
                 x.IsActive);

        if (membership is null)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_POOL_ACTIVE_MEMBERSHIP_NOT_FOUND",
                    "An active scouting pool membership was not found for the candidate."));
        }

        return membership.Remove(
            removedByAssignmentId,
            removalNote);
    }

    public Result UpdateCandidateMembershipNote(
        Guid scoutingCandidateId,
        string? note)
    {
        var membership = _memberships.FirstOrDefault(
            x => x.ScoutingCandidateId == scoutingCandidateId &&
                 x.IsActive);

        if (membership is null)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_POOL_ACTIVE_MEMBERSHIP_NOT_FOUND",
                    "An active scouting pool membership was not found for the candidate."));
        }

        membership.UpdateNote(note);

        return Result.Success();
    }

    public Result ChangeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_POOL_NAME_REQUIRED",
                    "Scouting pool name is required."));
        }

        Name = name.Trim();

        return Result.Success();
    }

    public void ChangeDescription(string? description)
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
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
