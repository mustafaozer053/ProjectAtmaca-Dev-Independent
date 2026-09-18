namespace ProjectAtmaca.Domain.TrainingTypes;

public interface ITrainingTypeRepository
{
    Task AddAsync(
        TrainingType trainingType,
        CancellationToken cancellationToken = default);

    Task<TrainingType?> GetByIdAsync(
        TrainingTypeId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingType>> ListAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
