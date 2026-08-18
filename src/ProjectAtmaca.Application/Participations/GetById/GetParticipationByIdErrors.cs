using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.GetById;

public static class GetParticipationByIdErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Participation.GetById.NotFound",
            "The requested participation was not found.");
}
