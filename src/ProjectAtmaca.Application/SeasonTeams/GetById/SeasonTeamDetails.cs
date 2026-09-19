namespace ProjectAtmaca.Application.SeasonTeams.GetById;

public sealed record SeasonTeamDetails(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipDetails> Memberships);

public sealed record SeasonTeamMembershipDetails(
    Guid Id,
    Guid AtmacaCardId,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive);
