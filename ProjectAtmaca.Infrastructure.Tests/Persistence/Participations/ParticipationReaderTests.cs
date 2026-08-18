using FluentAssertions;

using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Repositories;
using ProjectAtmaca.Infrastructure.Persistence.Readers;

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
}
