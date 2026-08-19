using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.RecordArrival;

public static class RecordParticipationArrivalErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Participation.RecordArrival.NotFound",
            "The requested participation was not found.");
}
