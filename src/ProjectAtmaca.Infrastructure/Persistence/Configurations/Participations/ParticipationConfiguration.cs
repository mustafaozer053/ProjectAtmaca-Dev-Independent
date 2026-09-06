using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Configurations.Participations;

public sealed class ParticipationConfiguration
    : IEntityTypeConfiguration<Participation>
{
    public void Configure(
        EntityTypeBuilder<Participation> builder)
    {
        builder.ToTable("Participations");

        builder.HasKey("Id");

        builder.Property("Id")
            .HasColumnName("Id")
            .ValueGeneratedNever();

        ConfigureAtmacaCardId(builder);

        ConfigureActivityReference(builder);

        ConfigureStatus(builder);

        ConfigureCondition(builder);

        ConfigureTemporalFacts(builder);

        ConfigureNote(builder);

        ConfigureAudit(builder);

        ConfigureConcurrency(builder);

        ConfigureIndexes(builder);

        builder.Ignore(x => x.ParticipationId);

        builder.Ignore(x => x.DomainEvents);
    }

    private static void ConfigureAtmacaCardId(
        EntityTypeBuilder<Participation> builder)
    {
        builder.Property(x => x.AtmacaCardId)
            .HasConversion(
                id => id.Value,
                value => AtmacaCardId.From(value))
            .HasColumnName("AtmacaCardId")
            .IsRequired();
    }

    private static void ConfigureActivityReference(
        EntityTypeBuilder<Participation> builder)
    {
        var comparer =
            new ValueComparer<ActivityReference>(
                (left, right) =>
                    left.ActivityType.Value ==
                        right.ActivityType.Value &&
                    left.ActivityId ==
                        right.ActivityId,

                value =>
                    HashCode.Combine(
                        value.ActivityType.Value,
                        value.ActivityId),

                value => value);

        builder.Property(x => x.ActivityReference)
            .HasConversion(
                value => SerializeActivityReference(value),
                value => DeserializeActivityReference(value))
            .HasColumnName("ActivityReference")
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired()
            .Metadata
            .SetValueComparer(comparer);
    }

    private static void ConfigureStatus(
        EntityTypeBuilder<Participation> builder)
    {
        builder.Property(x => x.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();
    }

    private static void ConfigureCondition(
        EntityTypeBuilder<Participation> builder)
    {
        builder.Property(x => x.Condition)
            .HasConversion(
                value => value == null
                    ? null
                    : value.Code,

                value => value == null
                    ? null
                    : ParticipationCondition
                        .Create(value)
                        .Value!)
            .HasColumnName("ConditionCode")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired(false);
    }

    private static void ConfigureTemporalFacts(
        EntityTypeBuilder<Participation> builder)
    {
        builder.Property(x => x.JoinedAt)
            .HasColumnName("JoinedAt");

        builder.Property(x => x.LeftAt)
            .HasColumnName("LeftAt");
    }

    private static void ConfigureNote(
        EntityTypeBuilder<Participation> builder)
    {
        builder.Property(x => x.Note)
            .HasConversion(
                value => value == null
                    ? null
                    : value.Value,

                value => value == null
                    ? null
                    : ParticipationNote
                        .Create(value)
                        .Value)
            .HasColumnName("Note")
            .HasMaxLength(
                ParticipationNote.MaxLength)
            .IsRequired(false);
    }

    private static void ConfigureAudit(
        EntityTypeBuilder<Participation> builder)
    {
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("CreatedAtUtc")
            .IsRequired();

        builder.Property<string?>("CreatedBy")
            .HasColumnName("CreatedBy")
            .HasMaxLength(200);

        builder.Property(x => x.LastModifiedAtUtc)
            .HasColumnName("LastModifiedAtUtc");

        builder.Property<string?>("LastModifiedBy")
            .HasColumnName("LastModifiedBy")
            .HasMaxLength(200);
    }

    private static void ConfigureConcurrency(
        EntityTypeBuilder<Participation> builder)
    {
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .HasColumnName("RowVersion");
    }

    private static void ConfigureIndexes(
        EntityTypeBuilder<Participation> builder)
    {
        builder.HasIndex(
                nameof(Participation.AtmacaCardId),
                nameof(Participation.ActivityReference))
            .IsUnique()
            .HasDatabaseName(
                "UX_Participations_AtmacaCard_Activity");
    }

    private static string SerializeActivityReference(
        ActivityReference activityReference)
    {
        return
            $"{activityReference.ActivityType.Value}:" +
            $"{activityReference.ActivityId:D}";
    }

    private static ActivityReference DeserializeActivityReference(
        string value)
    {
        string[] parts =
            value.Split(
                ':',
                2,
                StringSplitOptions.None);

        if (parts.Length != 2)
        {
            throw new InvalidOperationException(
                $"Invalid activity reference '{value}'.");
        }

        string activityTypeCode =
            parts[0];

        if (!Guid.TryParse(
                parts[1],
                out Guid activityId))
        {
            throw new InvalidOperationException(
                $"Invalid activity id in '{value}'.");
        }

        if (activityTypeCode ==
            ActivityTypeCode.TrainingCode)
        {
            return ActivityReference.ForTraining(
                TrainingId.From(activityId));
        }

        throw new InvalidOperationException(
            $"Unsupported persisted activity type " +
            $"'{activityTypeCode}'.");

        // Canonical actor audit persistence is introduced by Gate 5.7.3.
        builder.Ignore(
            participation =>
                participation.CreatedByActorId);

        builder.Ignore(
            participation =>
                participation.LastModifiedByActorId);
    }
}
