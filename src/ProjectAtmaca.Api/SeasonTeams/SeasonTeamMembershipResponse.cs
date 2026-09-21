namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamMembershipResponse(
    Guid Id,
    Guid AtmacaCardId,
    string? AtmacaCardDisplayName,
    string? AtmacaCardNumber,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipAssignmentResponse> Assignments);
