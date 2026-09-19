namespace ProjectAtmaca.Api.SeasonTeams;

public sealed record AddSeasonTeamMembershipRequest(
    Guid AtmacaCardId,
    DateTime StartDate,
    DateTime? EndDate);
