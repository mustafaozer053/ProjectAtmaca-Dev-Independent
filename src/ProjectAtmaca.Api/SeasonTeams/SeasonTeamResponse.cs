namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamResponse(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    int MembershipCount,
    bool IsActive);
