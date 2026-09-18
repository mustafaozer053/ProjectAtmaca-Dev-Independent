using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Trainings.GetById;

public static class GetTrainingByIdErrors
{
    public static readonly Error NotFound =
        Error.Create(
            "Training.GetById.NotFound",
            "The requested training was not found.");
}
