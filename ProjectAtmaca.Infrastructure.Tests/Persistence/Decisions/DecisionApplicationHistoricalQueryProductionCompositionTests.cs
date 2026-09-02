using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Decisions
    .ListApplicationHistory;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class
    DecisionApplicationHistoricalQueryProductionCompositionTests
{
    [Fact]
    public async Task Handle_Should_ReadHistoricalApplications_ThroughProductionComposition()
    {
        // Arrange
        DecisionId decisionId =
            DecisionId.New();

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                ParticipationId.New());

        DecisionApplication olderApplication =
            DecisionApplication.Create(
                decisionId,
                target,
                DecisionRevision.Initial,
                new DateTimeOffset(
                    2026,
                    9,
                    1,
                    8,
                    0,
                    0,
                    TimeSpan.Zero));

        DecisionApplication newerApplication =
            DecisionApplication.Create(
                decisionId,
                target,
                DecisionRevision.From(2),
                new DateTimeOffset(
                    2026,
                    9,
                    2,
                    9,
                    0,
                    0,
                    TimeSpan.Zero));

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext
                .Set<DecisionApplication>()
                .AddRange(
                    olderApplication,
                    newerApplication);

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

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        // Act
        Result<IReadOnlyList<DecisionApplicationHistoryItem>>
            result;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            ListDecisionApplicationHistoryQueryHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        ListDecisionApplicationHistoryQueryHandler>();

            result =
                await handler.Handle(
                    new ListDecisionApplicationHistoryQuery(
                        decisionId),
                    CancellationToken.None);
        }

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .HaveCount(2);

        result.Value
            .Select(
                item =>
                    item.DecisionApplicationId)
            .Should()
            .ContainInOrder(
                newerApplication.DecisionApplicationId,
                olderApplication.DecisionApplicationId);

        result.Value
            .Select(
                item =>
                    item.AppliedDecisionRevision)
            .Should()
            .ContainInOrder(
                DecisionRevision.From(2),
                DecisionRevision.Initial);
    }
}
