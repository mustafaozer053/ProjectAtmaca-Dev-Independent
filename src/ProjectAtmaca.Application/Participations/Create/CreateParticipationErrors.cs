using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application
    .Participations.Create;

public static class CreateParticipationErrors
{
    public static readonly Error AlreadyExists =
        Error.Create(
            "Participation.Create.AlreadyExists",
            "A participation already exists for the specified Atmaca card and activity.");
}
