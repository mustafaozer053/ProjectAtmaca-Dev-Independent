using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.SeasonTeams;

public sealed class SeasonTeamMembershipAssignment : Entity
{
    public SeasonTeamMembershipAssignmentId SeasonTeamMembershipAssignmentId =>
        SeasonTeamMembershipAssignmentId.From(Id);

    public SeasonTeamAssignmentKind Kind { get; private set; }

    public Guid DefinitionId { get; private set; }

    public string DisplayNameSnapshot { get; private set; }

    public AssignmentPeriod Period { get; private set; }

    private SeasonTeamMembershipAssignment()
    {
        DisplayNameSnapshot = null!;
        Period = null!;
    }

    private SeasonTeamMembershipAssignment(
        Guid id,
        SeasonTeamAssignmentKind kind,
        Guid definitionId,
        string displayNameSnapshot,
        AssignmentPeriod period)
        : base(id)
    {
        Kind = kind;
        DefinitionId = definitionId;
        DisplayNameSnapshot = displayNameSnapshot;
        Period = period;
    }

    internal static Result<SeasonTeamMembershipAssignment> Create(
        SeasonTeamAssignmentKind kind,
        Guid definitionId,
        string displayNameSnapshot,
        AssignmentPeriod period)
    {
        if (definitionId == Guid.Empty)
        {
            return Result<SeasonTeamMembershipAssignment>.Failure(
                SeasonTeamErrors.AssignmentDefinitionRequired);
        }

        if (string.IsNullOrWhiteSpace(displayNameSnapshot))
        {
            return Result<SeasonTeamMembershipAssignment>.Failure(
                SeasonTeamErrors.AssignmentDisplayNameRequired);
        }

        if (period is null)
        {
            return Result<SeasonTeamMembershipAssignment>.Failure(
                Error.Create(
                    "SEASON_TEAM_ASSIGNMENT_PERIOD_REQUIRED",
                    "Season team assignment period is required."));
        }

        return Result<SeasonTeamMembershipAssignment>.Success(
            new SeasonTeamMembershipAssignment(
                Guid.NewGuid(),
                kind,
                definitionId,
                displayNameSnapshot.Trim(),
                period));
    }

    public bool IsActiveOn(DateTime date)
    {
        return Period.IsActiveOn(date);
    }

    public Result End(DateTime endDate)
    {
        Result<AssignmentPeriod> result = Period.End(endDate);
        if (result.IsFailure)
            return Result.Failure(result.Error!);

        Period = result.Value!;
        return Result.Success();
    }
}
