using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Domain.Actors;
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
public sealed class MarkParticipationPresentIntegrationTests
{
    [Fact]
    public async Task Handle_Should_PersistPresentStatus_ThroughProductionComposition()
    {
        // Arrange
        Participation participation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New())
            .Value!;

        ParticipationId participationId =
            participation.ParticipationId;

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            await seedContext.SaveChangesAsync();
        }

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

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        ActorId currentActorId;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            using CancellationTokenSource permissionCancellationSource =
                new();

            currentActorId =
                await scope.ServiceProvider
                    .GrantPermissionToTestCurrentActorAsync(
                        Permissions.Participations.MarkPresent,
                        permissionCancellationSource.Token);

            MarkParticipationPresentCommandHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        MarkParticipationPresentCommandHandler>();

            var command =
                new MarkParticipationPresentCommand(
                    participationId,
                    null);

            // Act
            Result result =
                await handler.Handle(
                    command,
                    CancellationToken.None);

            // Assert — Application outcome
            result.IsSuccess
                .Should()
                .BeTrue();
        }

        // Assert — fresh DbContext proves durable SQL state,
        // not the tracked in-memory aggregate from the command scope.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? persistedParticipation =
            await verificationContext.Participations
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        participationId.Value,
                    CancellationToken.None);

        persistedParticipation
            .Should()
            .NotBeNull();

        persistedParticipation!.ParticipationId
            .Should()
            .Be(participationId);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.Present);

        persistedParticipation.Condition
            .Should()
            .BeNull();

        persistedParticipation.CreatedByActorId
            .Should()
            .BeNull(
                "the direct SQL seed did not pass through " +
                "production audit composition");

        persistedParticipation.LastModifiedByActorId
            .Should()
            .Be(currentActorId);
    }
}
