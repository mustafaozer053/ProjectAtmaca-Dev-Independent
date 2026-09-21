namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamMembershipAssignmentResponse(
    Guid Id,
    string Kind,
    Guid DefinitionId,
    string DisplayNameSnapshot,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive);
