using FluentAssertions;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Repositories;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ParticipationReaderTests
{
    [Fact]
    public async Task GetByIdAsync_Should_ReturnProjectedParticipation()
    {
        // Arrange
        Participation participation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New())
            .Value!;

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            await seedContext.SaveChangesAsync();
        }

        await using ProjectAtmacaDbContext queryContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        var reader =
            new ParticipationReader(
                queryContext);

        // Act
        ParticipationDetails? result =
            await reader.GetByIdAsync(
                participation.ParticipationId.Value,
                CancellationToken.None);

        // Assert
        result.Should()
            .NotBeNull();

        result!.Id.Should()
            .Be(participation.ParticipationId.Value);

        result.AtmacaCardId.Should()
            .Be(participation.AtmacaCardId.Value);

        result.ActivityTypeCode.Should()
            .Be(ActivityTypeCode.TrainingCode);

        result.ActivityId.Should()
            .Be(participation.ActivityReference.ActivityId);

        result.Status.Should()
            .Be(ParticipationStatus.NotRecorded);

        result.ConditionCode.Should()
            .BeNull();

        result.JoinedAt.Should()
            .BeNull();

        result.LeftAt.Should()
            .BeNull();

        result.Note.Should()
            .BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_PreservePersistedSemanticState()
    {
        // Arrange
        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                17,
                14,
                0,
                0,
                TimeSpan.Zero);

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        ParticipationNote note =
            ParticipationNote.Create(
                "Antrenmana geç katıldı ve erken ayrıldı.")
            .Value!;

        Participation participation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New())
            .Value!;

        participation.MarkPresent(
            ParticipationCondition.Late);

        participation.RecordArrival(
            joinedAt);

        participation.RecordDeparture(
            leftAt);

        participation.UpdateNote(
            note);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            await seedContext.SaveChangesAsync();
        }

        await using ProjectAtmacaDbContext queryContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        var reader =
            new ParticipationReader(
                queryContext);

        // Act
        ParticipationDetails? result =
            await reader.GetByIdAsync(
                participation.ParticipationId.Value,
                CancellationToken.None);

        // Assert
        result.Should()
            .NotBeNull();

        result!.Id.Should()
            .Be(participation.ParticipationId.Value);

        result.AtmacaCardId.Should()
            .Be(participation.AtmacaCardId.Value);

        result.ActivityTypeCode.Should()
            .Be(ActivityTypeCode.TrainingCode);

        result.ActivityId.Should()
            .Be(participation.ActivityReference.ActivityId);

        result.Status.Should()
            .Be(ParticipationStatus.Present);

        result.ConditionCode.Should()
            .Be(ParticipationCondition.Late.Code);

        result.JoinedAt.Should()
            .Be(joinedAt);

        result.LeftAt.Should()
            .Be(leftAt);

        result.Note.Should()
            .Be(note.Value);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_WhenParticipationDoesNotExist()
    {
        // Arrange
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        var reader =
            new ParticipationReader(
                context);

        Guid participationId =
            Guid.NewGuid();

        // Act
        ParticipationDetails? result =
            await reader.GetByIdAsync(
                participationId,
                CancellationToken.None);

        // Assert
        result.Should()
            .BeNull();
    }

    [Fact]
    public async Task ListByActivityAsync_Should_ReturnOnlyTargetActivityParticipations_InDeterministicOrder()
    {
        // Arrange
        ActivityReference targetActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        ActivityReference otherActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        Participation firstTargetParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation secondTargetParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation otherParticipation =
            Participation.Create(
                otherActivity,
                AtmacaCardId.New())
            .Value!;

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                20,
                10,
                0,
                0,
                TimeSpan.Zero);

        firstTargetParticipation
            .MarkPresent(
                ParticipationCondition.Late)
            .IsSuccess
            .Should()
            .BeTrue();

        firstTargetParticipation
            .RecordArrival(
                joinedAt)
            .IsSuccess
            .Should()
            .BeTrue();

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                firstTargetParticipation,
                secondTargetParticipation,
                otherParticipation);

            await seedContext.SaveChangesAsync();
        }

        await using ProjectAtmacaDbContext queryContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        IParticipationReader reader =
            new ParticipationReader(
                queryContext);

        // Act
        IReadOnlyList<ParticipationListItem> result =
            await reader.ListByActivityAsync(
                targetActivity,
                CancellationToken.None);

        // Assert
        result.Should()
            .HaveCount(2);

        result.Select(item => item.Id)
            .Should()
            .BeInAscendingOrder();

        result.Select(item => item.Id)
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    firstTargetParticipation
                        .ParticipationId.Value,
                    secondTargetParticipation
                        .ParticipationId.Value
                });

        result.Should()
            .NotContain(
                item =>
                    item.Id ==
                    otherParticipation
                        .ParticipationId.Value);

        ParticipationListItem firstProjection =
            result.Single(
                item =>
                    item.Id ==
                    firstTargetParticipation
                        .ParticipationId.Value);

        firstProjection.AtmacaCardId
            .Should()
            .Be(
                firstTargetParticipation
                    .AtmacaCardId.Value);

        firstProjection.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        firstProjection.ConditionCode
            .Should()
            .Be(
                ParticipationCondition.Late.Code);

        firstProjection.JoinedAt
            .Should()
            .Be(joinedAt);

        firstProjection.LeftAt
            .Should()
            .BeNull();

        ParticipationListItem secondProjection =
            result.Single(
                item =>
                    item.Id ==
                    secondTargetParticipation
                        .ParticipationId.Value);

        secondProjection.Status
            .Should()
            .Be(
                ParticipationStatus.NotRecorded);

        secondProjection.ConditionCode
            .Should()
            .BeNull();

        secondProjection.JoinedAt
            .Should()
            .BeNull();

        secondProjection.LeftAt
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task GetSummaryByActivityAsync_Should_ReturnExactStatusSummary_ForTargetActivity()
    {
        // Arrange
        ActivityReference targetActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        ActivityReference otherActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        Participation notRecordedParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation firstPresentParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation secondPresentParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation absentParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation otherPresentParticipation =
            Participation.Create(
                otherActivity,
                AtmacaCardId.New())
            .Value!;

        Participation otherAbsentParticipation =
            Participation.Create(
                otherActivity,
                AtmacaCardId.New())
            .Value!;

        firstPresentParticipation
            .MarkPresent()
            .IsSuccess
            .Should()
            .BeTrue();

        secondPresentParticipation
            .MarkPresent(
                ParticipationCondition.Late)
            .IsSuccess
            .Should()
            .BeTrue();

        absentParticipation
            .MarkAbsent()
            .IsSuccess
            .Should()
            .BeTrue();

        otherPresentParticipation
            .MarkPresent()
            .IsSuccess
            .Should()
            .BeTrue();

        otherAbsentParticipation
            .MarkAbsent()
            .IsSuccess
            .Should()
            .BeTrue();

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                notRecordedParticipation,
                firstPresentParticipation,
                secondPresentParticipation,
                absentParticipation,
                otherPresentParticipation,
                otherAbsentParticipation);

            await seedContext.SaveChangesAsync();
        }

        await using ProjectAtmacaDbContext queryContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        IParticipationReader reader =
            new ParticipationReader(
                queryContext);

        // Act
        ParticipationActivitySummary result =
            await reader.GetSummaryByActivityAsync(
                targetActivity,
                CancellationToken.None);

        // Assert
        result.Total
            .Should()
            .Be(4);

        result.NotRecorded
            .Should()
            .Be(1);

        result.Present
            .Should()
            .Be(2);

        result.Absent
            .Should()
            .Be(1);

        (
            result.NotRecorded +
            result.Present +
            result.Absent)
            .Should()
            .Be(result.Total);
    }

    [Fact]
    public async Task GetSummaryByActivityAsync_Should_ReturnZeroSummary_WhenNoMatchesExist()
    {
        // Arrange
        ActivityReference targetActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        await using ProjectAtmacaDbContext queryContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        IParticipationReader reader =
            new ParticipationReader(
                queryContext);

        // Act
        ParticipationActivitySummary result =
            await reader.GetSummaryByActivityAsync(
                targetActivity,
                CancellationToken.None);

        // Assert
        result.Total
            .Should()
            .Be(0);

        result.NotRecorded
            .Should()
            .Be(0);

        result.Present
            .Should()
            .Be(0);

        result.Absent
            .Should()
            .Be(0);

        (
            result.NotRecorded +
            result.Present +
            result.Absent)
            .Should()
            .Be(result.Total);
    }
}
