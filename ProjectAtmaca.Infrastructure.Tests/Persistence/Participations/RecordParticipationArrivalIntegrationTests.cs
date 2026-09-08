using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.RecordArrival;
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
public sealed class RecordParticipationArrivalIntegrationTests
{
    [Fact]
    public async Task Handle_Should_PersistJoinedAt_ThroughProductionComposition()
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

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                19,
                10,
                30,
                0,
                TimeSpan.Zero);

        participation.JoinedAt
            .Should()
            .BeNull();

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

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            await scope.ServiceProvider
                .GrantPermissionToTestCurrentActorAsync(
                    Permissions.Participations.RecordArrival,
                    CancellationToken.None);

            RecordParticipationArrivalCommandHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        RecordParticipationArrivalCommandHandler>();

            var command =
                new RecordParticipationArrivalCommand(
                    participationId,
                    joinedAt);

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

        persistedParticipation.JoinedAt
            .Should()
            .Be(joinedAt);
    }
}
