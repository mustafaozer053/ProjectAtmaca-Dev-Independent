using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Participations.GetById;

internal static class GetParticipationByIdEndpointErrors
{
    public static readonly Error InvalidParticipationId =
        Error.Create(
            "Participation.Id.Invalid",
            "Participation id must be a non-empty GUID in D format.");
}
