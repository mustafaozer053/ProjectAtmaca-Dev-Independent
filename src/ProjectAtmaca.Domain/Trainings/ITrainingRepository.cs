namespace ProjectAtmaca.Domain.Trainings;

public interface ITrainingRepository
{
    Task<Training?> GetByIdAsync(
        TrainingId id,
        CancellationToken cancellationToken = default);
}
