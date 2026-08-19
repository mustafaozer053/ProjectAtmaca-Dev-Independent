using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.RecordDeparture;

public static class RecordParticipationDepartureErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Participation.RecordDeparture.NotFound",
            "The requested participation was not found.");
}
