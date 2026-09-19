namespace ProjectAtmaca.Api.Trainings;

public sealed record TrainingListItemResponse(
    Guid Id,
    string Title,
    string Location,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    Guid SeasonId,
    Guid OrganizationId);
