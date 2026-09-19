namespace ProjectAtmaca.Application.SeasonTeams.Create;

public sealed record CreateSeasonTeamCommand(
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string? Name);
