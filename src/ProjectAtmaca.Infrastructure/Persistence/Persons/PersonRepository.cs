using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class PersonRepository(ProjectAtmacaDbContext dbContext) : IPersonRepository
{
    public Task<Person?> GetByIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<Person>()
            .Include(person => person.ProfessionalTitles)
                .ThenInclude(title => title.EvidenceDocuments)
            .SingleOrDefaultAsync(person => person.Id == personId, cancellationToken);
}
