using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Participations.CreateForTraining;

public sealed record CreateTrainingParticipationCommand(
    TrainingId TrainingId,
    AtmacaCardId AtmacaCardId);
