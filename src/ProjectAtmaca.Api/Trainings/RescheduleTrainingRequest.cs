namespace ProjectAtmaca.Api.Trainings;

public sealed record RescheduleTrainingRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyCollection<CreateTrainingAssignmentRequest> Assignments);
