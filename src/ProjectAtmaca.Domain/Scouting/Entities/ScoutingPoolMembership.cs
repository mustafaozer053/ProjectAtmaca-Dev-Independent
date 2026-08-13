using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Scouting.Entities;

public sealed class ScoutingPoolMembership : Entity
{
    public Guid ScoutingCandidateId { get; private set; }

    public DateTime AddedAtUtc { get; private set; }

    public Guid AddedByAssignmentId { get; private set; }

    public string? Note { get; private set; }

    public DateTime? RemovedAtUtc { get; private set; }

    public Guid? RemovedByAssignmentId { get; private set; }

    public string? RemovalNote { get; private set; }

    public bool IsActive => RemovedAtUtc is null;

    private ScoutingPoolMembership()
    {
    }

    private ScoutingPoolMembership(
        Guid id,
        Guid scoutingCandidateId,
        Guid addedByAssignmentId,
        string? note)
        : base(id)
    {
        ScoutingCandidateId = scoutingCandidateId;
        AddedAtUtc = DateTime.UtcNow;
        AddedByAssignmentId = addedByAssignmentId;
        Note = NormalizeOptionalText(note);
    }

    internal static Result<ScoutingPoolMembership> Create(
        Guid scoutingCandidateId,
        Guid addedByAssignmentId,
        string? note)
    {
        if (scoutingCandidateId == Guid.Empty)
        {
            return Result<ScoutingPoolMembership>.Failure(
                Error.Create(
                    "SCOUTING_POOL_MEMBERSHIP_CANDIDATE_REQUIRED",
                    "Scouting candidate id is required."));
        }

        if (addedByAssignmentId == Guid.Empty)
        {
            return Result<ScoutingPoolMembership>.Failure(
                Error.Create(
                    "SCOUTING_POOL_MEMBERSHIP_ADDED_BY_REQUIRED",
                    "Assignment id is required when adding a candidate to a scouting pool."));
        }

        return Result<ScoutingPoolMembership>.Success(
            new ScoutingPoolMembership(
                Guid.NewGuid(),
                scoutingCandidateId,
                addedByAssignmentId,
                note));
    }

    internal Result Remove(
        Guid removedByAssignmentId,
        string? removalNote)
    {
        if (!IsActive)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_POOL_MEMBERSHIP_ALREADY_REMOVED",
                    "Scouting pool membership has already been removed."));
        }

        if (removedByAssignmentId == Guid.Empty)
        {
            return Result.Failure(
                Error.Create(
                    "SCOUTING_POOL_MEMBERSHIP_REMOVED_BY_REQUIRED",
                    "Assignment id is required when removing a candidate from a scouting pool."));
        }

        RemovedAtUtc = DateTime.UtcNow;
        RemovedByAssignmentId = removedByAssignmentId;
        RemovalNote = NormalizeOptionalText(removalNote);

        return Result.Success();
    }

    internal void UpdateNote(string? note)
    {
        Note = NormalizeOptionalText(note);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
