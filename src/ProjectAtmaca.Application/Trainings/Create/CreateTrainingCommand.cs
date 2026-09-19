namespace ProjectAtmaca.Application.Trainings.Create;

public sealed record CreateTrainingCommand(
    Guid SeasonId,
    Guid OrganizationId,
    string Title,
    string? Description,
    string Location,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyCollection<TrainingTypeAssignmentInput> Assignments,
    Guid SeasonTeamId = default);

public sealed record TrainingTypeAssignmentInput(
    Guid TrainingTypeId,
    int DurationMinutes);
