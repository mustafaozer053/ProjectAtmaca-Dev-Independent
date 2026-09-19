namespace ProjectAtmaca.Api.Trainings;

public sealed record CreateTrainingRequest(
    Guid SeasonId,
    Guid OrganizationId,
    string Title,
    string? Description,
    string Location,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyCollection<CreateTrainingAssignmentRequest> Assignments,
    Guid SeasonTeamId = default);

public sealed record CreateTrainingAssignmentRequest(
    Guid TrainingTypeId,
    int DurationMinutes);
