namespace ProjectAtmaca.Api.Trainings;

public sealed record GetTrainingByIdResponse(
    Guid Id,
    string Title,
    string Description,
    string Location,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    Guid SeasonId,
    Guid OrganizationId,
    Guid? SeasonTeamId);
