using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;
[Collection(SqlIntegrationCollection.Name)]
public sealed class ParticipationPersistenceRoundTripTests
{
    [Fact]
    public async Task Participation_Should_RoundTrip_ThroughSqlServer()
    {
        await using ProjectAtmacaDbContext setupContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation participation =
            Participation.Create(
                activityReference,
                atmacaCardId).Value!;

        ParticipationCorrectionReason reason =
            ParticipationCorrectionReason.Create(
                "Test correction reason.").Value!;

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                12,
                10,
                0,
                0,
                TimeSpan.FromHours(3));

        DateTimeOffset correctedJoinedAt =
            joinedAt.AddMinutes(5);

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        participation.RecordArrival(joinedAt);
        participation.CorrectArrival(
            correctedJoinedAt,
            reason);
        participation.RecordDeparture(leftAt);

        ParticipationNote note =
            ParticipationNote.Create(
                "Integration test note.").Value!;

        participation.UpdateNote(note);

        participation.MarkPresent(
            ParticipationCondition.Late);

        setupContext.Participations.Add(
            participation);

        await setupContext.SaveChangesAsync();

        ParticipationId id =
            participation.ParticipationId;

        await using ProjectAtmacaDbContext reloadContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? reloaded =
            await reloadContext.Participations
                .FindAsync(
                [id.Value]);

        reloaded.Should().NotBeNull();

        reloaded!.ParticipationId.Should()
            .Be(id);

        reloaded.AtmacaCardId.Should()
            .Be(atmacaCardId);

        reloaded.ActivityReference.Should()
            .Be(activityReference);

        reloaded.Status.Should()
            .Be(ParticipationStatus.Present);

        reloaded.Condition.Should()
            .Be(ParticipationCondition.Late);

        reloaded.JoinedAt.Should()
            .Be(correctedJoinedAt);

        reloaded.LeftAt.Should()
            .Be(leftAt);

        reloaded.Note.Should()
            .Be(note);
    }
}
