using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.AtmacaCards;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class AtmacaCardDocumentConfiguration
    : IEntityTypeConfiguration<AtmacaCardDocument>
{
    public void Configure(EntityTypeBuilder<AtmacaCardDocument> builder)
    {
        builder.ToTable("AtmacaCardDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AtmacaCardId).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Issuer).HasMaxLength(150);
        builder.Property(x => x.IssuedOn).HasColumnType("date");
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FileSizeBytes).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.AtmacaCardId);
        builder.HasOne<AtmacaCard>()
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.AtmacaCardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
