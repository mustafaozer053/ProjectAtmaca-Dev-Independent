namespace ProjectAtmaca.Domain.TrainingTypes;

public interface ITrainingTypeRepository
{
    Task<TrainingType?> GetByIdAsync(
        TrainingTypeId id,
        CancellationToken cancellationToken = default);
}
