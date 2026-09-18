using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Participations;

internal static class ParticipationEndpointErrors
{
    public static readonly Error InvalidParticipationId =
        Error.Create(
            "Participation.Id.Invalid",
            "Participation id must be a non-empty GUID in D format.");

    public static readonly Error InvalidActivityId =
        Error.Create(
            "Participation.ActivityId.Invalid",
            "Activity id must be a non-empty GUID in D format.");

    public static readonly Error InvalidAtmacaCardId =
        Error.Create(
            "Participation.AtmacaCardId.Invalid",
            "Atmaca card id must be a non-empty GUID in D format.");

    public static readonly Error InvalidHistoryCursor =
        Error.Create(
            "ParticipationHistory.Cursor.Invalid",
            "Cursor must contain a valid Atmaca Card id, UTC creation timestamp, and non-empty participation id.");
}
