using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.GetById;

public sealed record TrainingDetails(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TrainingStatus Status,
    Guid SeasonId,
    Guid OrganizationId,
    Guid? SeasonTeamId);
