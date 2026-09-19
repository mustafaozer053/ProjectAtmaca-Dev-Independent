using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Participations.CreateForTraining;

public static class CreateTrainingParticipationErrors
{
    public static readonly Error TrainingNotFound =
        Error.Create(
            "Participation.Training.NotFound",
            "Training was not found.");

    public static readonly Error SeasonTeamRequired =
        Error.Create(
            "Participation.Training.SeasonTeamRequired",
            "Training is not linked to a season team.");

    public static readonly Error SeasonTeamInactive =
        Error.Create(
            "Participation.Training.SeasonTeamInactive",
            "Participation cannot be created for an inactive season team.");

    public static readonly Error MembershipNotActive =
        Error.Create(
            "Participation.SeasonTeam.MembershipNotActive",
            "Atmaca card has no active membership in the training season team on the training date.");
}
