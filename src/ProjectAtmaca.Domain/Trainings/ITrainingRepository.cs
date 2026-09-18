namespace ProjectAtmaca.Domain.Trainings;

public interface ITrainingRepository
{
    Task AddAsync(
        Training training,
        CancellationToken cancellationToken = default);

    Task<Training?> GetByIdAsync(
        TrainingId id,
        CancellationToken cancellationToken = default);
}
