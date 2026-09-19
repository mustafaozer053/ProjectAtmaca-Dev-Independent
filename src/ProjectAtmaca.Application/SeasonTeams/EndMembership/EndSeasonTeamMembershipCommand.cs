namespace ProjectAtmaca.Application.SeasonTeams.EndMembership;

public sealed record EndSeasonTeamMembershipCommand(
    Guid SeasonTeamId,
    Guid MembershipId,
    DateTime EndDate);
