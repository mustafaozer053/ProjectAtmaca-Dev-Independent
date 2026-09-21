namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamRosterViewResponse(
    Guid SeasonTeamId,
    IReadOnlyList<SeasonTeamRosterGroupResponse> Groups,
    IReadOnlyList<SeasonTeamMembershipResponse> UnclassifiedMemberships);

public sealed record SeasonTeamRosterGroupResponse(
    string Classification,
    IReadOnlyList<SeasonTeamMembershipResponse> Memberships);
