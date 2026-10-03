using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Participations;

public static class BtaEligibilityErrors
{
    public static readonly Error RequiresOtherActiveTeam =
        Error.Create(
            "Participation.Bta.RequiresOtherActiveTeam",
            "BTA can only be recorded when the athlete has an active membership in another active season team on the training date.");
}
