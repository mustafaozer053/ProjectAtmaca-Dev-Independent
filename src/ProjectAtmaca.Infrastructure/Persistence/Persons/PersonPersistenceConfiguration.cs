using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

internal static class RegistrationMapping
{
    public static void Audit<T>(EntityTypeBuilder<T> b) where T : AuditableAggregateRoot
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Ignore(x => x.DomainEvents);
        var actor = new ValueConverter<ActorId, Guid>(x => x.Value, x => ActorId.From(x));
        b.Property(x => x.CreatedByActorId).HasConversion(actor);
        b.Property(x => x.LastModifiedByActorId).HasConversion(actor);
        b.Property(x => x.CreatedAtUtc).HasConversion(x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
    }
}
public sealed class PersonPersistenceConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("Persons");
        RegistrationMapping.Audit(b);
        b.Property(x => x.Name).HasConversion(x => x.FullName, x => PersonName.Create(x).Value!).HasMaxLength(150);
        b.Property(x => x.BirthDate).HasConversion(x => x.Value, x => BirthDate.Create(x)).HasColumnType("date");
        b.Property(x => x.BirthCountry).HasConversion(x => RegistrationJson.Write(CountryData.From(x)),
            x => RegistrationJson.Read<CountryData>(x).ToDomain());
        b.Property(x => x.BirthPlace).HasConversion(x => RegistrationJson.Write(LocationData.From(x!)),
            x => RegistrationJson.Read<LocationData>(x).ToDomain());
        b.Property(x => x.MotherName).HasConversion(x => x!.FullName, x => PersonName.Create(x).Value!).HasMaxLength(150);
        b.Property(x => x.FatherName).HasConversion(x => x!.FullName, x => PersonName.Create(x).Value!).HasMaxLength(150);
        b.Property(x => x.Email).HasConversion(x => x!.Value, x => Email.Create(x));
        b.Property(x => x.PrimaryPhoneNumber).HasConversion(x => RegistrationJson.Write(PhoneData.From(x!)),
            x => RegistrationJson.Read<PhoneData>(x).ToDomain());
        b.Property(x => x.SecondaryPhoneNumber).HasConversion(x => RegistrationJson.Write(PhoneData.From(x!)),
            x => RegistrationJson.Read<PhoneData>(x).ToDomain());
        b.Property(x => x.Address).HasConversion(x => RegistrationJson.Write(AddressData.From(x!)),
            x => RegistrationJson.Read<AddressData>(x).ToDomain());
    }
}
public sealed class PersonRegistrationPersistenceConfiguration : IEntityTypeConfiguration<PersonRegistration>
{
    public void Configure(EntityTypeBuilder<PersonRegistration> b)
    {
        b.ToTable("PersonRegistrations");
        RegistrationMapping.Audit(b);
        b.Property<string>("NationalIdentityNumber").HasMaxLength(11);
        b.Property<string>("PassportMatchKey").HasMaxLength(64).IsUnicode(false);
        b.HasIndex("PassportMatchKey");
        b.HasIndex("NationalIdentityNumber").IsUnique()
            .HasFilter("[NationalIdentityNumber] IS NOT NULL");
        b.Property(x => x.Identity).HasConversion(x => RegistrationJson.Write(IdentityData.From(x)),
            x => RegistrationJson.Read<IdentityData>(x).ToDomain());
        b.HasIndex(x => x.PersonId).IsUnique();
        b.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class RegistrationCardConfiguration : IEntityTypeConfiguration<AtmacaCard>
{
    public void Configure(EntityTypeBuilder<AtmacaCard> b)
    {
        b.ToTable("AtmacaCards");
        RegistrationMapping.Audit(b);
        b.Ignore(x => x.AtmacaCardId);
        b.Property(x => x.CardNumber).HasConversion(x => x.Value, x => AtmacaCardNumber.Create(x).Value!).HasMaxLength(10);
        b.Property(x => x.IssuedAtUtc).HasConversion(x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
        b.HasIndex(x => x.CardNumber).IsUnique();
        b.HasIndex(x => x.PersonId).IsUnique();
        b.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class PersonRegistrationOperationConfiguration : IEntityTypeConfiguration<PersonRegistrationOperation>
{
    public void Configure(EntityTypeBuilder<PersonRegistrationOperation> b)
    {
        b.ToTable("PersonRegistrationOperations");
        b.HasKey(x => new { x.ActorId, x.OperationId });
        b.Property(x => x.CardNumber).HasMaxLength(10);
        b.Property(x => x.IssuedAtUtc).HasConversion(x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
        b.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AtmacaCard>().WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Restrict);
    }
}
