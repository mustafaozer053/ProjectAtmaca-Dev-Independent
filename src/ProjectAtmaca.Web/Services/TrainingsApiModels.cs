namespace ProjectAtmaca.Web.Services;

public sealed record TrainingListItemResponse(
    Guid Id,
    string Title,
    string Location,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    Guid SeasonId,
    Guid OrganizationId,
    Guid? SeasonTeamId);

public sealed record TrainingTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int DisplayOrder,
    bool IsActive);
