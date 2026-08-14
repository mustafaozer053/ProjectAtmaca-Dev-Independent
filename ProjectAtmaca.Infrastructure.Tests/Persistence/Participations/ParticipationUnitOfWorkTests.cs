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

        await writeContext.Database
            .EnsureDeletedAsync();

        await writeContext.Database
            .MigrateAsync();

        ParticipationRepository repository =
            new(writeContext);

        UnitOfWork unitOfWork =
            new(writeContext);

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Result<ProjectAtmaca.Domain.Participations.Participation> creationResult =
    ProjectAtmaca.Domain.Participations.Participation.Create(
        activityReference,
        atmacaCardId);

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        ProjectAtmaca.Domain.Participations.Participation participation =
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
}
