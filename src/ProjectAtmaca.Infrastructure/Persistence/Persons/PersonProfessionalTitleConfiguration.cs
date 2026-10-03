using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class PersonProfessionalTitleConfiguration
    : IEntityTypeConfiguration<PersonProfessionalTitle>
{
    public void Configure(EntityTypeBuilder<PersonProfessionalTitle> builder)
    {
        builder.ToTable("PersonProfessionalTitles");
        builder.HasKey(title => title.Id);
        builder.Property(title => title.Id).ValueGeneratedNever();
        builder.Property(title => title.PersonId).IsRequired();
        builder.Property(title => title.Title).HasMaxLength(150).IsRequired();
        builder.Property(title => title.StartedOn).HasColumnType("date");
        builder.Property(title => title.EndedOn).HasColumnType("date");
        builder.HasIndex(title => new { title.PersonId, title.Title });
        builder.HasOne<Person>()
            .WithMany(person => person.ProfessionalTitles)
            .HasForeignKey(title => title.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PersonProfessionalTitleEvidenceDocumentConfiguration
    : IEntityTypeConfiguration<PersonProfessionalTitleEvidenceDocument>
{
    public void Configure(EntityTypeBuilder<PersonProfessionalTitleEvidenceDocument> builder)
    {
        builder.ToTable("PersonProfessionalTitleEvidenceDocuments");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.Property(link => link.PersonProfessionalTitleId).IsRequired();
        builder.Property(link => link.AtmacaCardDocumentId).IsRequired();
        builder.HasIndex(link => new
        {
            link.PersonProfessionalTitleId,
            link.AtmacaCardDocumentId
        }).IsUnique();
        builder.HasOne<PersonProfessionalTitle>()
            .WithMany(title => title.EvidenceDocuments)
            .HasForeignKey(link => link.PersonProfessionalTitleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AtmacaCardDocument>()
            .WithMany()
            .HasForeignKey(link => link.AtmacaCardDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
