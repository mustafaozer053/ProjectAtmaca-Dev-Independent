using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.SeasonTeams;

public sealed class SeasonTeamMembership : Entity
{
    private readonly List<SeasonTeamMembershipAssignment> _assignments = [];

    public SeasonTeamMembershipId SeasonTeamMembershipId =>
        SeasonTeamMembershipId.From(Id);

    public AtmacaCardId AtmacaCardId { get; private set; }

    public AssignmentPeriod Period { get; private set; }

    public IReadOnlyCollection<SeasonTeamMembershipAssignment> Assignments =>
        _assignments.AsReadOnly();

    private SeasonTeamMembership()
    {
        Period = null!;
    }

    private SeasonTeamMembership(
        Guid id,
        AtmacaCardId atmacaCardId,
        AssignmentPeriod period)
        : base(id)
    {
        AtmacaCardId = atmacaCardId;
        Period = period;
    }

    internal static Result<SeasonTeamMembership> Create(
        AtmacaCardId atmacaCardId,
        AssignmentPeriod period)
    {
        if (atmacaCardId.Value == Guid.Empty)
        {
            return Result<SeasonTeamMembership>.Failure(
                Error.Create(
                    "SEASON_TEAM_MEMBERSHIP_ATMACA_CARD_REQUIRED",
                    "Atmaca card id is required."));
        }

        if (period is null)
        {
            return Result<SeasonTeamMembership>.Failure(
                Error.Create(
                    "SEASON_TEAM_MEMBERSHIP_PERIOD_REQUIRED",
                    "Season team membership period is required."));
        }

        return Result<SeasonTeamMembership>.Success(
            new SeasonTeamMembership(
                Guid.NewGuid(),
                atmacaCardId,
                period));
    }

    public bool IsActiveOn(DateTime date)
    {
        return Period.IsActiveOn(date);
    }

    public Result<SeasonTeamMembershipAssignment> AddAssignment(
        SeasonTeamAssignmentKind kind,
        Guid definitionId,
        string displayNameSnapshot,
        AssignmentPeriod period)
    {
        if (_assignments.Any(
                x => x.Kind == kind &&
                     x.DefinitionId == definitionId &&
                     PeriodsOverlap(x.Period, period)))
        {
            return Result<SeasonTeamMembershipAssignment>.Failure(
                SeasonTeamErrors.DuplicateAssignment);
        }

        Result<SeasonTeamMembershipAssignment> assignmentResult =
            SeasonTeamMembershipAssignment.Create(
                kind,
                definitionId,
                displayNameSnapshot,
                period);
        if (assignmentResult.IsFailure)
        {
            return Result<SeasonTeamMembershipAssignment>.Failure(
                assignmentResult.Error!);
        }

        SeasonTeamMembershipAssignment assignment = assignmentResult.Value!;
        _assignments.Add(assignment);
        return Result<SeasonTeamMembershipAssignment>.Success(assignment);
    }

    public Result End(DateTime endDate)
    {
        var result = Period.End(endDate);

        if (result.IsFailure)
        {
            return Result.Failure(result.Error!);
        }

        Period = result.Value!;
        return Result.Success();
    }

    public Result EndAssignment(
        SeasonTeamMembershipAssignmentId assignmentId,
        DateTime endDate)
    {
        SeasonTeamMembershipAssignment? assignment = _assignments
            .SingleOrDefault(x => x.SeasonTeamMembershipAssignmentId == assignmentId);
        if (assignment is null)
            return Result.Failure(SeasonTeamErrors.AssignmentNotFound);

        return assignment.End(endDate);
    }

    private static bool PeriodsOverlap(
        AssignmentPeriod first,
        AssignmentPeriod second)
    {
        DateTime firstEnd = first.EndDate ?? DateTime.MaxValue.Date;
        DateTime secondEnd = second.EndDate ?? DateTime.MaxValue.Date;

        return first.StartDate <= secondEnd &&
               second.StartDate <= firstEnd;
    }
}
