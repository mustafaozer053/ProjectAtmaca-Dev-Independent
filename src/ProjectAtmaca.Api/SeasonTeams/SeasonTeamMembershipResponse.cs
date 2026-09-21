namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamMembershipResponse(
    Guid Id,
    Guid AtmacaCardId,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipAssignmentResponse> Assignments);
