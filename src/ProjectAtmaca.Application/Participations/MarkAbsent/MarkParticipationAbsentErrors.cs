using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Participations.MarkAbsent;

public static class MarkParticipationAbsentErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Participation.MarkAbsent.NotFound",
            "Participation was not found.");
}
