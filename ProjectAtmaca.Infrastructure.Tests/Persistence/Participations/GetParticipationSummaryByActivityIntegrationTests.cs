using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
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
public sealed class GetParticipationSummaryByActivityIntegrationTests
{
    [Fact]
    public async Task Handle_Should_ReturnExactSummary_ThroughProductionComposition()
    {
        // Arrange
        ActivityReference targetActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        ActivityReference otherActivity =
            ActivityReference.ForTraining(
                TrainingId.New());

        Participation notRecordedParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation firstPresentParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation secondPresentParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation absentParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation btaParticipation =
            Participation.Create(
                targetActivity,
                AtmacaCardId.New())
            .Value!;

        Participation otherPresentParticipation =
            Participation.Create(
                otherActivity,
                AtmacaCardId.New())
            .Value!;

        firstPresentParticipation
            .MarkPresent()
            .IsSuccess
            .Should()
            .BeTrue();

        secondPresentParticipation
            .MarkPresent(
                ParticipationCondition.Late)
            .IsSuccess
            .Should()
            .BeTrue();

        absentParticipation
            .MarkAbsent()
            .IsSuccess
            .Should()
            .BeTrue();

        btaParticipation
            .MarkAbsent(
                ParticipationCondition.Bta)
            .IsSuccess
            .Should()
            .BeTrue();

        otherPresentParticipation
            .MarkPresent()
            .IsSuccess
            .Should()
            .BeTrue();

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                notRecordedParticipation,
                firstPresentParticipation,
                secondPresentParticipation,
                absentParticipation,
                btaParticipation,
                otherPresentParticipation);

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

        GetParticipationSummaryByActivityQuery query =
            new(
                targetActivity);

        Result<ParticipationActivitySummary> result;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            await scope.ServiceProvider
                .GrantPermissionToTestCurrentActorAsync(
                    Permissions.Participations
                        .GetSummaryByActivity,
                    CancellationToken.None);

            GetParticipationSummaryByActivityQueryHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        GetParticipationSummaryByActivityQueryHandler>();

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
            .NotBeNull();

        result.Value!.Total
            .Should()
            .Be(4);

        result.Value.NotRecorded
            .Should()
            .Be(1);

        result.Value.Present
            .Should()
            .Be(2);

        result.Value.Absent
            .Should()
            .Be(1);

        (
            result.Value.NotRecorded +
            result.Value.Present +
            result.Value.Absent)
            .Should()
            .Be(result.Value.Total);
    }
}
