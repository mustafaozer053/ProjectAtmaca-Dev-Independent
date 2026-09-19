using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.SeasonTeams;

public static class SeasonTeamApplicationErrors
{
    public static readonly Error NotFound =
        Error.Create("SeasonTeam.NotFound", "Season team was not found.");
}
