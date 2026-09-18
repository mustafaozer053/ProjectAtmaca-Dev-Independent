using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.Cancel;

public sealed record CancelTrainingCommand(
    TrainingId TrainingId);
