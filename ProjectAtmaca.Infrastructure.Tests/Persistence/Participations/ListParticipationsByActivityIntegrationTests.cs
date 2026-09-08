using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.ListByActivity;
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
public sealed class ListParticipationsByActivityIntegrationTests
{
    [Fact]
    public async Task Handle_Should_ReturnOnlyTargetActivityParticipations_ThroughProductionComposition()
    {
        // Arrange
        ActivityReference targetActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        ActivityReference otherActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        Participation firstTargetParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation secondTargetParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation otherParticipation =
            Participation.Create(
                otherActivity,
                AtmacaCardId.New())
            .Value!;

        DateTimeOffset joinedAt =
            new(
                2026,
                8,
                24,
                9,
                0,
                0,
                TimeSpan.Zero);

        Result markPresentResult =
            firstTargetParticipation.MarkPresent(
                ParticipationCondition.Late);

        markPresentResult.IsSuccess
            .Should()
            .BeTrue();

        Result arrivalResult =
            firstTargetParticipation.RecordArrival(
                joinedAt);

        arrivalResult.IsSuccess
            .Should()
            .BeTrue();

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                firstTargetParticipation,
                secondTargetParticipation,
                otherParticipation);

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

        var query =
            new ListParticipationsByActivityQuery(
                targetActivity);

        Result<IReadOnlyList<ParticipationListItem>>
            result;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            await scope.ServiceProvider
                .GrantPermissionToTestCurrentActorAsync(
                    Permissions.Participations.ListByActivity,
                    CancellationToken.None);

            ListParticipationsByActivityQueryHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationsByActivityQueryHandler>();

            // Act
            result =
                await handler.Handle(
                    query,
                    CancellationToken.None);
        }

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .HaveCount(2);

        result.Value!
            .Select(item => item.Id)
            .Should()
            .BeInAscendingOrder();

        result.Value
            .Select(item => item.Id)
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    firstTargetParticipation
                        .ParticipationId.Value,
                    secondTargetParticipation
                        .ParticipationId.Value
                });

        result.Value
            .Should()
            .NotContain(
                item =>
                    item.Id ==
                    otherParticipation
                        .ParticipationId.Value);

        ParticipationListItem firstProjection =
            result.Value.Single(
                item =>
                    item.Id ==
                    firstTargetParticipation
                        .ParticipationId.Value);

        firstProjection.Status
            .Should()
            .Be(
                ParticipationStatus.Present);

        firstProjection.ConditionCode
            .Should()
            .Be(
                ParticipationCondition.Late.Code);

        firstProjection.JoinedAt
            .Should()
            .Be(joinedAt);

        firstProjection.LeftAt
            .Should()
            .BeNull();
    }
}
