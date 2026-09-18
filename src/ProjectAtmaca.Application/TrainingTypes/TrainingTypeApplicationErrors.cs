using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.TrainingTypes;

public static class TrainingTypeApplicationErrors
{
    public static readonly Error NotFound = Error.Create(
        "TrainingType.NotFound",
        "The requested training type was not found.");
}
