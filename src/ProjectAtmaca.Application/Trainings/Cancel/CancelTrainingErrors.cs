using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Trainings.Cancel;

public static class CancelTrainingErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Training.Cancel.NotFound",
            "The requested training was not found.");
}
