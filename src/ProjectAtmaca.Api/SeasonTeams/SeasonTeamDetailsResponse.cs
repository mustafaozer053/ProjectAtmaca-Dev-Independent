namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamDetailsResponse(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    bool IsActive,
    DateTime SeasonStartDate,
    DateTime SeasonEndDate,
    IReadOnlyList<SeasonTeamMembershipResponse> Memberships);
