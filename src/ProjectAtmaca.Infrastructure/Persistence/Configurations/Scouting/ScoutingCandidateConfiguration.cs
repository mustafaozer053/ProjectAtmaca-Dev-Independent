using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Scouting;
using ProjectAtmaca.Domain.Scouting.Entities;
using ProjectAtmaca.Domain.Scouting.ValueObjects;
using ProjectAtmaca.Infrastructure.Persistence.Persons;

namespace ProjectAtmaca.Infrastructure.Persistence.Configurations.Scouting;

public sealed record ScoutingSourceData(int SourceType, string? ReferrerName, string? Description);

public sealed class ScoutingCandidateConfiguration : IEntityTypeConfiguration<ScoutingCandidate>
{
    public void Configure(EntityTypeBuilder<ScoutingCandidate> b)
    {
        b.ToTable("ScoutingCandidates");
        RegistrationMapping.Audit(b);
        b.Property(x => x.Name).HasConversion(x => x.FullName, x => PersonName.Create(x).Value!).HasMaxLength(150).IsRequired();
        b.Property(x => x.BirthDate).HasConversion(x => x!.Value, x => BirthDate.Create(x)).HasColumnType("date");
        b.Property(x => x.LicenseNumber).HasConversion(x => x!.Value, x => LicenseNumber.Create(x).Value!).HasMaxLength(50);
        b.Property(x => x.Email).HasConversion(x => x!.Value, x => Email.Create(x)).HasMaxLength(200);
        b.Property(x => x.BirthPlace).HasConversion(x => RegistrationJson.Write(LocationData.From(x!)),
            x => RegistrationJson.Read<LocationData>(x).ToDomain());
        b.Property(x => x.Nationality).HasConversion(x => RegistrationJson.Write(CountryData.From(x!)),
            x => RegistrationJson.Read<CountryData>(x).ToDomain());
        b.Property(x => x.PrimaryPhoneNumber).HasConversion(x => RegistrationJson.Write(PhoneData.From(x!)),
            x => RegistrationJson.Read<PhoneData>(x).ToDomain());
        b.Property(x => x.SecondaryPhoneNumber).HasConversion(x => RegistrationJson.Write(PhoneData.From(x!)),
            x => RegistrationJson.Read<PhoneData>(x).ToDomain());
        b.Property(x => x.Address).HasConversion(x => RegistrationJson.Write(AddressData.From(x!)),
            x => RegistrationJson.Read<AddressData>(x).ToDomain());
        b.Property(x => x.InitialSource).HasConversion(
            x => RegistrationJson.Write(new ScoutingSourceData((int)x.SourceType, x.ReferrerName == null ? null : x.ReferrerName.FullName, x.SourceDescription)),
            x => ToSource(RegistrationJson.Read<ScoutingSourceData>(x))).IsRequired();
        b.Property(x => x.ScoutingDecision).HasConversion<int>().IsRequired();
        b.Property(x => x.IdentityNumber).HasConversion(
            x => x!.CountryCode + "|" + (int)x.IdentityType + "|" + x.Number,
            x => ToIdentity(x)).HasMaxLength(60);
        b.Property<string>("IdentityKey").HasMaxLength(80);
        b.HasIndex("IdentityKey").IsUnique().HasFilter("[IdentityKey] IS NOT NULL");
        b.HasMany(x => x.Observations).WithOne().HasForeignKey("ScoutingCandidateId").IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Observations).HasField("_observations").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Property<byte[]>("RowVersion").IsRowVersion();
    }

    private static IdentityNumber ToIdentity(string value)
    {
        var parts = value.Split('|');
        return IdentityNumber.Create(parts[0], (IdentityType)int.Parse(parts[1]), parts[2]).Value!;
    }

    private static InitialScoutingSource ToSource(ScoutingSourceData data) =>
        InitialScoutingSource.Create(
            (ProjectAtmaca.Domain.Common.Enums.InitialScoutingSourceType)data.SourceType,
            data.ReferrerName is null ? null : PersonName.Create(data.ReferrerName).Value,
            data.Description).Value!;
}

public sealed class ScoutingObservationConfiguration : IEntityTypeConfiguration<ScoutingObservation>
{
    public void Configure(EntityTypeBuilder<ScoutingObservation> b)
    {
        b.ToTable("ScoutingObservations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ObservedOn).HasColumnType("date");
        b.Property(x => x.ObservationType).HasConversion<int>();
        b.Property(x => x.ObservedEvent).HasMaxLength(200);
        b.Property(x => x.ObservedClub).HasMaxLength(150);
        b.Property(x => x.ObservedTeam).HasMaxLength(150);
        b.Property(x => x.DominantFoot).HasConversion<int?>();
        b.Property(x => x.Strengths).HasMaxLength(1000);
        b.Property(x => x.Weaknesses).HasMaxLength(1000);
        b.Property(x => x.ObserverRecommendation).HasConversion<int?>();
        b.Property(x => x.RecommendationNote).HasMaxLength(1000);
        b.Property(x => x.ObserverName).HasConversion(x => x.FullName, x => PersonName.Create(x).Value!).HasMaxLength(150);
        b.Property(x => x.ObservedLocation).HasConversion(x => RegistrationJson.Write(LocationData.From(x!)),
            x => RegistrationJson.Read<LocationData>(x).ToDomain());
        b.Ignore(x => x.PositionIds);
        b.Property<List<Guid>>("_positionIds").HasColumnName("PositionIds").HasMaxLength(2000)
            .HasConversion(
                x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null),
                x => string.IsNullOrWhiteSpace(x) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(x, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<List<Guid>>(
                    (l, r) => l!.SequenceEqual(r!),
                    v => v.Aggregate(0, (h, g) => HashCode.Combine(h, g.GetHashCode())),
                    v => v.ToList()));
    }
}
