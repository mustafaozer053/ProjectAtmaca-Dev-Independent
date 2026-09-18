namespace ProjectAtmaca.Api.TrainingTypes;

public sealed record TrainingTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int DisplayOrder,
    bool IsActive);
