namespace ProjectAtmaca.Domain.Persons;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
