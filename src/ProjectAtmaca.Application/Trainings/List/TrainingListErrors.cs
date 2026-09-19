using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Trainings.List;

public static class TrainingListErrors
{
    public static readonly Error InvalidDateRange = Error.Create(
        "Training.List.InvalidDateRange",
        "The training list start date cannot be after the end date.");
}
