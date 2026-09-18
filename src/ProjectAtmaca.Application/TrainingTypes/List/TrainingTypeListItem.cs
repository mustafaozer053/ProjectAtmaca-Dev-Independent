namespace ProjectAtmaca.Application.TrainingTypes.List;

public sealed record TrainingTypeListItem(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int DisplayOrder,
    bool IsActive);
