using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.RecordDeparture;
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
public sealed class RecordParticipationDepartureIntegrationTests
{
    [Fact]
    public async Task Handle_Should_PersistLeftAt_AndPreserveJoinedAt_ThroughProductionComposition()
    {
        // Arrange
        Result<Participation> creationResult =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New());

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        Participation participation =
            creationResult.Value!;

        ParticipationId participationId =
            participation.ParticipationId;

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                19,
                10,
                0,
                0,
                TimeSpan.Zero);

        DateTimeOffset leftAt =
            joinedAt.AddHours(2);

        Result arrivalResult =
            participation.RecordArrival(
                joinedAt);

        arrivalResult.IsSuccess
            .Should()
            .BeTrue();

        participation.JoinedAt
            .Should()
            .Be(joinedAt);

        participation.LeftAt
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
                .AddInMemoryCollection(
                    configurationValues)
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
                    Permissions.Participations.RecordDeparture,
                    CancellationToken.None);

            RecordParticipationDepartureCommandHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        RecordParticipationDepartureCommandHandler>();

            var command =
                new RecordParticipationDepartureCommand(
                    participationId,
                    leftAt);

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
        // independently from the command scope's tracked aggregate.
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

        persistedParticipation.LeftAt
            .Should()
            .Be(leftAt);
    }
}
