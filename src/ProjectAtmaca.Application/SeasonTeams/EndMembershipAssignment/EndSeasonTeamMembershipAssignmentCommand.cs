namespace ProjectAtmaca.Application.SeasonTeams.EndMembershipAssignment;

public sealed record EndSeasonTeamMembershipAssignmentCommand(
    Guid SeasonTeamId,
    Guid MembershipId,
    Guid AssignmentId,
    DateTime EndDate);
