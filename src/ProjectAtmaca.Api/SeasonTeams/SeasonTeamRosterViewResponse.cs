namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record SeasonTeamRosterViewResponse(
    Guid SeasonTeamId,
    int TotalMembershipCount,
    int ActiveMembershipCount,
    int InactiveMembershipCount,
    int UnclassifiedMembershipCount,
    IReadOnlyList<SeasonTeamRosterPrimarySectionResponse> PrimaryRosterSections,
    IReadOnlyList<SeasonTeamRosterGroupResponse> Groups,
    IReadOnlyList<SeasonTeamRosterClassificationCountResponse> ClassificationCounts,
    IReadOnlyList<SeasonTeamMembershipResponse> UnclassifiedMemberships);

public sealed record SeasonTeamRosterGroupResponse(
    string Classification,
    int MembershipCount,
    IReadOnlyList<SeasonTeamMembershipResponse> Memberships);

public sealed record SeasonTeamRosterClassificationCountResponse(
    string Classification,
    int MembershipCount);

public sealed record SeasonTeamRosterPrimarySectionResponse(
    string SectionKey,
    string Label,
    int MembershipCount,
    bool HasMembers);
