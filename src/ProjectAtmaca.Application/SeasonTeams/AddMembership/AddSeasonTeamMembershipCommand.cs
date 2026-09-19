namespace ProjectAtmaca.Application.SeasonTeams.AddMembership;

public sealed record AddSeasonTeamMembershipCommand(
    Guid SeasonTeamId,
    Guid AtmacaCardId,
    DateTime StartDate,
    DateTime? EndDate);
