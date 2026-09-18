namespace ProjectAtmaca.Api.TrainingTypes;

public sealed record CreateTrainingTypeRequest(
    string? Code,
    string? Name,
    string? Description,
    int DisplayOrder);
