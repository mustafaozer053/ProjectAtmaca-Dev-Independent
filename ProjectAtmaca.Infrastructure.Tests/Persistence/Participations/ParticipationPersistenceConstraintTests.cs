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
public sealed class ParticipationPersistenceConstraintTests
{
    [Fact]
    public async Task SaveChanges_Should_Reject_Duplicate_SubjectContext()
    {
        // Arrange
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        Participation firstParticipation =
            Participation.Create(
                activityReference,
                atmacaCardId).Value!;

        Participation duplicateParticipation =
            Participation.Create(
                activityReference,
                atmacaCardId).Value!;

        context.Participations.Add(
            firstParticipation);

        await context.SaveChangesAsync();

        context.Participations.Add(
            duplicateParticipation);

        // Act
        Func<Task> action =
            async () =>
                await context.SaveChangesAsync();

        // Assert
        await action.Should()
            .ThrowAsync<DbUpdateException>();

        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        int persistedCount =
            await verificationContext.Participations
                .CountAsync(
                    participation =>
                        participation.AtmacaCardId ==
                            atmacaCardId);

        persistedCount.Should()
            .Be(1);
    }
    [Fact]
    public async Task SaveChanges_Should_RollBackAllChanges_WhenAnyTrackedChangeFails()
    {
        // Arrange
        AtmacaCardId existingAtmacaCardId =
            AtmacaCardId.New();

        ActivityReference existingActivityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {

            Participation existingParticipation =
                Participation.Create(
                    existingActivityReference,
                    existingAtmacaCardId).Value!;

            seedContext.Participations.Add(
                existingParticipation);

            await seedContext.SaveChangesAsync();
        }

        AtmacaCardId validAtmacaCardId =
            AtmacaCardId.New();

        ActivityReference validActivityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        await using (
            ProjectAtmacaDbContext context =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            Participation validParticipation =
                Participation.Create(
                    validActivityReference,
                    validAtmacaCardId).Value!;

            Participation duplicateParticipation =
                Participation.Create(
                    existingActivityReference,
                    existingAtmacaCardId).Value!;

            context.Participations.Add(
                validParticipation);

            context.Participations.Add(
                duplicateParticipation);

            // Act
            Func<Task> action =
                async () =>
                    await context.SaveChangesAsync();

            // Assert
            await action.Should()
                .ThrowAsync<DbUpdateException>();
        }

        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        int existingCount =
            await verificationContext.Participations
                .CountAsync(
                    participation =>
                        participation.AtmacaCardId ==
                            existingAtmacaCardId);

        int validCount =
            await verificationContext.Participations
                .CountAsync(
                    participation =>
                        participation.AtmacaCardId ==
                            validAtmacaCardId);

        existingCount.Should()
            .Be(1);

        validCount.Should()
            .Be(0);
    }
}
