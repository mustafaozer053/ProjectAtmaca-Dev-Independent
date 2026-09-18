using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.TrainingTypes.ChangeStatus;

public sealed record ChangeTrainingTypeStatusCommand(
    TrainingTypeId TrainingTypeId,
    bool IsActive);
