namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record CreateSeasonTeamRequest(
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string? Name);
