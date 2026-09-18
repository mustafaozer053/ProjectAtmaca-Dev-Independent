using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Trainings.Confirm;

public static class ConfirmTrainingErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Training.Confirm.NotFound",
            "The requested training was not found.");
}
