namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamDetailsResponse(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipResponse> Memberships);
