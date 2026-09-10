using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Participations;

internal static class ParticipationEndpointErrors
{
    public static readonly Error InvalidParticipationId =
        Error.Create(
            "Participation.Id.Invalid",
            "Participation id must be a non-empty GUID in D format.");
}
