using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.AddMembershipAssignment;

public sealed record AddSeasonTeamMembershipAssignmentCommand(
    Guid SeasonTeamId,
    Guid MembershipId,
    SeasonTeamAssignmentKind Kind,
    Guid DefinitionId,
    string DisplayNameSnapshot,
    DateTime StartDate,
    DateTime? EndDate);
