using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Repositories;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;
[Collection(SqlIntegrationCollection.Name)]
public sealed class ParticipationRepositoryTests
{
    [Fact]
    public async Task AddAsync_Should_PersistParticipation()
    {
        // Arrange
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ParticipationRepository repository =
            new(context);

        Participation participation =
            CreateParticipation();

        // Act
        await repository.AddAsync(
            participation);

        await context.SaveChangesAsync();

        ParticipationId id =
            participation.ParticipationId;

        // Assert
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? persisted =
            await verificationContext.Participations
                .FindAsync([id.Value]);

        persisted.Should()
            .NotBeNull();

        persisted!.ParticipationId
            .Should()
            .Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnPersistedParticipation()
    {
        // Arrange
        ParticipationId id;

        await using (
            ProjectAtmacaDbContext setupContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {

            Participation participation =
                CreateParticipation();

            setupContext.Participations.Add(
                participation);

            await setupContext.SaveChangesAsync();

            id =
                participation.ParticipationId;
        }

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ParticipationRepository repository =
            new(context);

        // Act
        Participation? result =
            await repository.GetByIdAsync(id);

        // Assert
        result.Should()
            .NotBeNull();

        result!.ParticipationId
            .Should()
            .Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_WhenParticipationDoesNotExist()
    {
        // Arrange
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ParticipationRepository repository =
            new(context);

        ParticipationId missingId =
            ParticipationId.New();

        // Act
        Participation? result =
            await repository.GetByIdAsync(
                missingId);

        // Assert
        result.Should()
            .BeNull();
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnTrue_ForExistingSubjectContext()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation participation =
            Participation.Create(
                activityReference,
                atmacaCardId).Value!;

        context.Participations.Add(
            participation);

        await context.SaveChangesAsync();

        ParticipationRepository repository =
            new(context);

        // Act
        bool exists =
            await repository.ExistsAsync(
                atmacaCardId,
                activityReference);

        // Assert
        exists.Should()
            .BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnFalse_ForMissingSubjectContext()
    {
        // Arrange
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ParticipationRepository repository =
            new(context);

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        // Act
        bool exists =
            await repository.ExistsAsync(
                atmacaCardId,
                activityReference);

        // Assert
        exists.Should()
            .BeFalse();
    }

    private static Participation CreateParticipation()
    {
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        return Participation.Create(
            activityReference,
            atmacaCardId).Value!;
    }
}
