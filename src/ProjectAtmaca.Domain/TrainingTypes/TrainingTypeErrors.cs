using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.TrainingTypes;

public static class TrainingTypeErrors
{
    public static readonly Error CodeEmpty = Error.Create(
        "TrainingType.Code.Empty",
        "Training type code cannot be empty.");

    public static Error CodeTooLong(int maxLength)
    {
        return Error.Create(
            "TrainingType.Code.TooLong",
            $"Training type code cannot exceed {maxLength} characters.");
    }

    public static readonly Error CodeMustStartWithLetter = Error.Create(
        "TrainingType.Code.MustStartWithLetter",
        "Training type code must start with a letter.");

    public static readonly Error CodeInvalidCharacters = Error.Create(
        "TrainingType.Code.InvalidCharacters",
        "Training type code can contain only letters, numbers, and underscores.");

    public static readonly Error NameEmpty = Error.Create(
        "TrainingType.Name.Empty",
        "Training type name cannot be empty.");

    public static Error NameTooLong(int maxLength)
    {
        return Error.Create(
            "TrainingType.Name.TooLong",
            $"Training type name cannot exceed {maxLength} characters.");
    }
    public static Error DescriptionTooLong(int maxLength)
    {
        return Error.Create(
            "TrainingType.Description.TooLong",
            $"Training type description cannot exceed {maxLength} characters.");
    }

    public static readonly Error DisplayOrderInvalid = Error.Create(
        "TrainingType.DisplayOrder.Invalid",
        "Training type display order cannot be negative.");

    public static readonly Error TrainingTypeRequired = Error.Create(
        "Training.Type.Required",
        "A valid training type is required.");
}
