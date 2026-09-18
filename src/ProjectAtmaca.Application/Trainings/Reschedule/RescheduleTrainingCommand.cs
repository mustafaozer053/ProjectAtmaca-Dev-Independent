using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Application.Trainings.Create;

namespace ProjectAtmaca.Application.Trainings.Reschedule;

public sealed record RescheduleTrainingCommand(
    TrainingId TrainingId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyCollection<TrainingTypeAssignmentInput> Assignments);
