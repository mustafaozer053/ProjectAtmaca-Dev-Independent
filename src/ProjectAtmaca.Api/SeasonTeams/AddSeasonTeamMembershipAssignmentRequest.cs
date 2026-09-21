using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record AddSeasonTeamMembershipAssignmentRequest(
    SeasonTeamAssignmentKind Kind,
    Guid DefinitionId,
    string DisplayNameSnapshot,
    DateTime StartDate,
    DateTime? EndDate);
