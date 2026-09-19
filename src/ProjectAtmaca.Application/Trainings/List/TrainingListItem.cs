using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.List;

public sealed record TrainingListItem(
    Guid Id,
    string Title,
    string Location,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TrainingStatus Status,
    Guid SeasonId,
    Guid OrganizationId);
