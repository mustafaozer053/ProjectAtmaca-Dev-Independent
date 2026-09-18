using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Trainings.Create;

public static class TrainingCreationErrors
{
    public static readonly Error AssignmentsRequired =
        Error.Create(
            "Training.Create.AssignmentsRequired",
            "At least one training assignment is required.");

    public static readonly Error OrganizationContextRequired =
        Error.Create(
            "Training.Create.OrganizationContextRequired",
            "Season and organization are required.");

    public static readonly Error TrainingTypeRequired =
        Error.Create(
            "Training.Create.TrainingTypeRequired",
            "Every training assignment must have a training type.");

    public static readonly Error TrainingTypeNotFound =
        Error.Create(
            "Training.Create.TrainingTypeNotFound",
            "The selected training type does not exist.");

    public static readonly Error TrainingTypeInactive =
        Error.Create(
            "Training.Create.TrainingTypeInactive",
            "The selected training type is inactive.");
}
