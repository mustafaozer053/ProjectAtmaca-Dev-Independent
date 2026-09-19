using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Trainings.List;

public sealed record ListTrainingsQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    Guid? SeasonId = null,
    Guid? OrganizationId = null,
    TrainingStatus? Status = null);
