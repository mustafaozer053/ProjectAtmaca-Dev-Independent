using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests.Persistence;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ParticipationDependencyInjectionTests
{
    [Fact]
    public async Task AddInfrastructure_Should_ShareScopedDbContext_BetweenRepositoryAndUnitOfWork()
    {
        // Arrange
        await using ProjectAtmacaDbContext setupContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        await setupContext.Database
            .EnsureDeletedAsync();

        await setupContext.Database
            .MigrateAsync();

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddInfrastructure(configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationId participationId;

        await using (AsyncServiceScope scope =
            serviceProvider.CreateAsyncScope())
        {
            IParticipationRepository repository =
                scope.ServiceProvider
                    .GetRequiredService<IParticipationRepository>();

            IUnitOfWork unitOfWork =
                scope.ServiceProvider
                    .GetRequiredService<IUnitOfWork>();

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

            participationId =
                participation.ParticipationId;

            await repository.AddAsync(
                participation);

            int affectedRows =
                await unitOfWork.SaveChangesAsync();

            affectedRows.Should()
                .BeGreaterThan(0);
        }

        // Fresh DbContext:
        // DI scope dışındaki gerçek SQL persistence'ını kanıtlar.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? persistedParticipation =
            await verificationContext.Participations
                .SingleOrDefaultAsync(
                    participation =>
                        participation.Id ==
                        participationId.Value);

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
