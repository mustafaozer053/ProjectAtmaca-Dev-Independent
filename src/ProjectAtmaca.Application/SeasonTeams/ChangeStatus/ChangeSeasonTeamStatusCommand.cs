using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.ChangeStatus;

public sealed record ChangeSeasonTeamStatusCommand(
    SeasonTeamId SeasonTeamId,
    bool IsActive);
