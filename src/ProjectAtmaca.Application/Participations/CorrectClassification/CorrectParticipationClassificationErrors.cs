using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Participations.CorrectClassification;

public static class CorrectParticipationClassificationErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Participation.CorrectClassification.NotFound",
            "The requested participation was not found.");
}
