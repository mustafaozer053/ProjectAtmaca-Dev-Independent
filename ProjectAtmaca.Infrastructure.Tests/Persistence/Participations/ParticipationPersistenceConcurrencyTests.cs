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
public sealed class ParticipationPersistenceConcurrencyTests
{
    [Fact]
    public async Task SaveChanges_Should_Reject_StaleParticipationUpdate()
    {
        // Arrange
        ParticipationId participationId;

        await using (
            ProjectAtmacaDbContext setupContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            await setupContext.Database.MigrateAsync();

            AtmacaCardId atmacaCardId =
                AtmacaCardId.New();

            ActivityReference activityReference =
                ActivityReference.ForTraining(
                    TrainingId.New());

            Participation participation =
                Participation.Create(
                    activityReference,
                    atmacaCardId).Value!;

            setupContext.Participations.Add(
                participation);

            await setupContext.SaveChangesAsync();

            participationId =
                participation.ParticipationId;
        }

        await using ProjectAtmacaDbContext firstContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        await using ProjectAtmacaDbContext staleContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation firstCopy =
            await firstContext.Participations.FindAsync(
                [participationId.Value])
            ?? throw new InvalidOperationException(
                "Participation was not found.");

        Participation staleCopy =
            await staleContext.Participations.FindAsync(
                [participationId.Value])
            ?? throw new InvalidOperationException(
                "Participation was not found.");

        ParticipationNote firstNote =
            ParticipationNote.Create(
                "First concurrent update.").Value!;

        firstCopy.UpdateNote(
            firstNote);

        await firstContext.SaveChangesAsync();

        ParticipationNote staleNote =
            ParticipationNote.Create(
                "Stale concurrent update.").Value!;

        staleCopy.UpdateNote(
            staleNote);

        // Act
        Func<Task> action =
            async () =>
                await staleContext.SaveChangesAsync();

        // Assert
        await action.Should()
            .ThrowAsync<DbUpdateConcurrencyException>();

        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation persisted =
            await verificationContext.Participations.FindAsync(
                [participationId.Value])
            ?? throw new InvalidOperationException(
                "Participation was not found.");

        persisted.Note.Should()
            .Be(firstNote);
    }
}
