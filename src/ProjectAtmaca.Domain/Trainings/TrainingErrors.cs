using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Trainings;

public static class TrainingErrors
{
    public static readonly Error TitleEmpty = Error.Create(
        "Training.Title.Empty",
        "Training title cannot be empty.");

    public static Error TitleTooLong(int maxLength)
    {
        return Error.Create(
            "Training.Title.TooLong",
            $"Training title cannot exceed {maxLength} characters.");
    }
    public static Error DescriptionTooLong(int maxLength)
    {
        return Error.Create(
            "Training.Description.TooLong",
            $"Training description cannot exceed {maxLength} characters.");
    }
    public static readonly Error LocationEmpty = Error.Create(
    "Training.Location.Empty",
    "Training location cannot be empty.");

    public static Error LocationTooLong(int maxLength)
    {
        return Error.Create(
            "Training.Location.TooLong",
            $"Training location cannot exceed {maxLength} characters.");
    }
    public static readonly Error DurationTooShort =
        Error.Create(
            "Training.Duration.TooShort",
            "Training duration must be at least 15 minutes.");

    public static readonly Error DurationTooLong =
        Error.Create(
            "Training.Duration.TooLong",
            "Training duration cannot exceed 240 minutes.");

    public static readonly Error DurationInvalid =
        Error.Create(
            "Training.Duration.Invalid",
            "Training duration must be greater than zero.");

    public static readonly Error TrainingTypeAssignmentRequired =
        Error.Create(
            "Training.TypeAssignment.Required",
            "A training must have at least one training type.");

    public static readonly Error DuplicateTrainingTypeAssignment =
        Error.Create(
            "Training.TypeAssignment.Duplicate",
            "A training cannot contain duplicate training types.");

    public static readonly Error ScheduleEndMustBeAfterStart =
        Error.Create(
            "Training.Schedule.EndMustBeAfterStart",
            "Training end time must be later than start time.");

    public static Error ScheduleTooShort(int minimumMinutes)
    {
        return Error.Create(
            "Training.Schedule.TooShort",
            $"Training duration must be at least {minimumMinutes} minutes.");
    }

    public static Error ScheduleTooLong(int maximumMinutes)
    {
        return Error.Create(
            "Training.Schedule.TooLong",
            $"Training duration cannot exceed {maximumMinutes} minutes.");
    }

    public static readonly Error TrainingTypeDurationTooShort =
        Error.Create(
            "Training.TypeDuration.TooShort",
            "Training type duration must be at least 1 minute.");

    public static Error TrainingTypeDurationTooLong(int maximumMinutes)
    {
        return Error.Create(
            "Training.TypeDuration.TooLong",
            $"Training type duration cannot exceed {maximumMinutes} minutes.");
    }

    public static readonly Error TrainingTypeDurationMismatch =
        Error.Create(
            "Training.TypeDuration.Mismatch",
            "The total duration of training types must equal the training duration.");

}

