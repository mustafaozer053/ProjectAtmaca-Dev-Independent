using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Repositories;
using ProjectAtmaca.Infrastructure.Tests.Persistence;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ParticipationUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_Should_CommitRepositoryChanges_ToSqlServer()
    {
        // Arrange
        await using ProjectAtmacaDbContext writeContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ParticipationRepository repository =
            new(writeContext);

        UnitOfWork unitOfWork =
            new(writeContext);

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Result<Participation> creationResult =
        Participation.Create(
        activityReference,
        atmacaCardId);

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        Participation participation =
        creationResult.Value!;

        Result markPresentResult =
            participation.MarkPresent();

        markPresentResult.IsSuccess
            .Should()
            .BeTrue();

        await repository.AddAsync(
            participation);

        // Act
        int affectedRows =
            await unitOfWork.SaveChangesAsync();

        // Assert — Unit of Work actually persisted something
        affectedRows.Should()
            .BeGreaterThan(0);

        ParticipationId participationId =
            participation.ParticipationId;

        // A completely new DbContext is intentional.
        // We must prove SQL persistence, not EF change tracking.
        await using ProjectAtmacaDbContext readContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ParticipationRepository readRepository =
            new(readContext);

        Participation? persistedParticipation =
            await readRepository.GetByIdAsync(
                participationId);

        persistedParticipation.Should()
            .NotBeNull();

        persistedParticipation!.ParticipationId
            .Should()
            .Be(participationId);

        persistedParticipation.ActivityReference
            .Should()
            .Be(activityReference);

        persistedParticipation.AtmacaCardId
            .Should()
            .Be(atmacaCardId);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.Present);
    }
    [Fact]
    public async Task SaveChangesAsync_Should_RollBackAllChanges_WhenAnyTrackedChangeFails()
    {
        // Arrange — establish a committed row that will later
        // be used to trigger the database uniqueness constraint.
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
            ParticipationRepository seedRepository =
                new(seedContext);

            UnitOfWork seedUnitOfWork =
                new(seedContext);

            Result<Participation> seedCreationResult =
                Participation.Create(
                    existingActivityReference,
                    existingAtmacaCardId);

            seedCreationResult.IsSuccess
                .Should()
                .BeTrue();

            await seedRepository.AddAsync(
                seedCreationResult.Value!);

            await seedUnitOfWork.SaveChangesAsync();
        }

        AtmacaCardId validAtmacaCardId =
            AtmacaCardId.New();

        ActivityReference validActivityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            ParticipationRepository repository =
                new(writeContext);

            UnitOfWork unitOfWork =
                new(writeContext);

            Result<Participation> validCreationResult =
                Participation.Create(
                    validActivityReference,
                    validAtmacaCardId);

            validCreationResult.IsSuccess
                .Should()
                .BeTrue();

            Result<Participation> duplicateCreationResult =
                Participation.Create(
                    existingActivityReference,
                    existingAtmacaCardId);

            duplicateCreationResult.IsSuccess
                .Should()
                .BeTrue();

            await repository.AddAsync(
                validCreationResult.Value!);

            await repository.AddAsync(
                duplicateCreationResult.Value!);

            // Act
            Func<Task> action =
                async () =>
                    await unitOfWork.SaveChangesAsync();

            // Assert — the database constraint must reject
            // the entire Unit of Work persistence operation.
            await action.Should()
                .ThrowAsync<DbUpdateException>();
        }

        // A completely new DbContext is intentional.
        // We must verify committed SQL state, not EF tracking state.
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
