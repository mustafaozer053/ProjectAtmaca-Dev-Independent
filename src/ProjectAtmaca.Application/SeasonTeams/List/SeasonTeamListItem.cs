namespace ProjectAtmaca.Application.SeasonTeams.List;

public sealed record SeasonTeamListItem(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    int MembershipCount,
    bool IsActive);
