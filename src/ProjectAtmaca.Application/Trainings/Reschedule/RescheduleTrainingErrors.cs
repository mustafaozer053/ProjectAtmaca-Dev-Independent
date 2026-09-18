using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Trainings.Reschedule;

public static class RescheduleTrainingErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Training.Reschedule.NotFound",
            "The requested training was not found.");
}
