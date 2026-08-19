using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.MarkPresent;

public static class MarkParticipationPresentErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Participation.MarkPresent.NotFound",
            "The requested participation was not found.");
}
