using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;

namespace ProjectAtmaca.Infrastructure.Persistence
    .Configurations.Decisions;

public sealed class DecisionConfiguration
    : IEntityTypeConfiguration<Decision>
{
    public void Configure(
        EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("Decisions");

        builder.HasKey("Id");

        builder.Property("Id")
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(x => x.Revision)
            .HasConversion(
                revision =>
                    revision.Value,

                value =>
                    DecisionRevision.From(value))
            .HasColumnName("Revision")
            .IsRequired();

        builder.Property(x => x.Effect)
            .HasConversion(
                effect =>
                    (int)effect.Outcome,

                value =>
                    DeserializeEffect(value))
            .HasColumnName("EffectOutcome")
            .IsRequired();

        builder.Ignore(
            x => x.DecisionId);

        builder.Ignore(
            x => x.DomainEvents);

        ConfigureSnapshot(builder);

        builder.Property(x => x.SupersededByDecisionId)
            .HasConversion(
                decisionId =>
                    decisionId.HasValue
                        ? decisionId.Value.Value
                        : (Guid?)null,

                value =>
                    value.HasValue
                        ? DecisionId.From(value.Value)
                        : (DecisionId?)null)
            .HasColumnName("SupersededByDecisionId")
            .IsRequired(false);

        builder.ComplexProperty(
            x => x.Target,
            target =>
            {
                target.Property(x => x.TargetType)
                    .HasConversion<int>()
                    .HasColumnName("TargetType")
                    .IsRequired();

                target.Property(x => x.TargetId)
                    .HasColumnName("TargetId")
                    .IsRequired();
            });
    }

    private static ParticipationClassificationEffect DeserializeEffect(
        int value)
    {
        return value switch
        {
            (int)ParticipationClassificationOutcome.Present =>
                ParticipationClassificationEffect.Present(),

            (int)ParticipationClassificationOutcome.Absent =>
                ParticipationClassificationEffect.Absent(),

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported persisted participation " +
                    $"classification outcome '{value}'.")
        };
    }
    private static void ConfigureSnapshot(
    EntityTypeBuilder<Decision> builder)
    {
        builder.ComplexProperty(
            x => x.Snapshot,
            snapshot =>
            {
                var activityReferenceComparer =
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

                snapshot.Property(x => x.ActivityReference)
                    .HasConversion(
                        value =>
                            SerializeActivityReference(value),

                        value =>
                            DeserializeActivityReference(value))
                    .HasColumnName(
                        "SnapshotActivityReference")
                    .HasMaxLength(64)
                    .IsUnicode(false)
                    .IsRequired()
                    .Metadata
                    .SetValueComparer(
                        activityReferenceComparer);

                snapshot.Property(x => x.AtmacaCardId)
                    .HasConversion(
                        id => id.Value,
                        value => AtmacaCardId.From(value))
                    .HasColumnName(
                        "SnapshotAtmacaCardId")
                    .IsRequired();

                snapshot.Property(x => x.Status)
                    .HasConversion<int>()
                    .HasColumnName(
                        "SnapshotStatus")
                    .IsRequired();

                snapshot.Property(x => x.Condition)
                    .HasConversion(
                        value => value == null
                            ? null
                            : value.Code,

                        value => value == null
                            ? null
                            : ParticipationCondition
                                .Create(value)
                                .Value!)
                    .HasColumnName(
                        "SnapshotConditionCode")
                    .HasMaxLength(32)
                    .IsUnicode(false)
                    .IsRequired(false);

                snapshot.Property(x => x.JoinedAt)
                    .HasColumnName(
                        "SnapshotJoinedAt");

                snapshot.Property(x => x.LeftAt)
                    .HasColumnName(
                        "SnapshotLeftAt");
            });
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
    }
}
