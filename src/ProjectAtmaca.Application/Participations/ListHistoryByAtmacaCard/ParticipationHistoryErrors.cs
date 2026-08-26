using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.ListHistoryByAtmacaCard;

public static class ParticipationHistoryErrors
{
    public static readonly Error InvalidPageSize =
        Error.Create(
            "ParticipationHistory.InvalidPageSize",
            "Page size must be between 1 and 100.");

    public static readonly Error CursorScopeMismatch =
        Error.Create(
            "ParticipationHistory.CursorScopeMismatch",
            "The cursor does not belong to the requested Atmaca Card.");
}
