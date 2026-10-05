using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Scouting;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Infrastructure.Persistence.Readers;

public sealed class ScoutingRegistrationLookup(ProjectAtmacaDbContext dbContext) : IScoutingRegistrationLookup
{
    public async Task<Guid?> FindPersonIdByNationalIdAsync(
        string nationalIdentityNumber, CancellationToken cancellationToken = default)
    {
        var personId = await dbContext.Set<PersonRegistration>().AsNoTracking()
            .Where(x => EF.Property<string>(x, "NationalIdentityNumber") == nationalIdentityNumber)
            .Select(x => (Guid?)x.PersonId)
            .FirstOrDefaultAsync(cancellationToken);
        return personId;
    }

    public async Task<IReadOnlyList<Guid>> FindSimilarPersonIdsAsync(
        string fullName, DateOnly? birthDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return [];

        var name = PersonName.Create(fullName.Trim());
        if (name.IsFailure)
            return [];

        var personName = name.Value!;
        var query = dbContext.Set<Person>().AsNoTracking().Where(x => x.Name == personName);
        if (birthDate is not null)
        {
            var date = BirthDate.Create(birthDate.Value.ToDateTime(TimeOnly.MinValue));
            query = query.Where(x => x.BirthDate == date);
        }
        return await query.Select(x => x.Id).Take(5).ToListAsync(cancellationToken);
    }
}