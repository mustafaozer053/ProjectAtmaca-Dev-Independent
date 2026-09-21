namespace ProjectAtmaca.Web.Services;

// Client-side mirrors of ProjectAtmaca.Api.SeasonTeams response contracts.
// Kept separate from the API project so the Web app only depends on the
// wire shape (JSON) rather than referencing the Api assembly directly.

public sealed record SeasonTeamResponse(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    int MembershipCount,
    bool IsActive);

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

public sealed record SeasonTeamMembershipResponse(
    Guid Id,
    Guid AtmacaCardId,
    string? AtmacaCardDisplayName,
    string? AtmacaCardNumber,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipAssignmentResponse> Assignments);

public sealed record SeasonTeamMembershipAssignmentResponse(
    Guid Id,
    string Kind,
    Guid DefinitionId,
    string DisplayNameSnapshot,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive);
