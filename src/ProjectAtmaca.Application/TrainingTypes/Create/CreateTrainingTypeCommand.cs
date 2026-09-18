namespace ProjectAtmaca.Application.TrainingTypes.Create;

public sealed record CreateTrainingTypeCommand(
    string? Code,
    string? Name,
    string? Description,
    int DisplayOrder);
