using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class PersonCitizenshipConfiguration : IEntityTypeConfiguration<PersonCitizenship>
{
    public void Configure(EntityTypeBuilder<PersonCitizenship> b)
    {
        b.ToTable("PersonCitizenships");
        RegistrationMapping.Audit(b);
        b.Property(x => x.Country).HasConversion(x => RegistrationJson.Write(CountryData.From(x)),
            x => RegistrationJson.Read<CountryData>(x).ToDomain());
        b.Property(x => x.AcquiredOn).HasColumnType("date");
        b.HasIndex(x => x.PersonId);
        b.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
