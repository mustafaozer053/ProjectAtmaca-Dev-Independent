using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Decisions.ListApplicationHistory;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class DecisionApplicationHistoricalReaderTests
{
    [Fact]
    public async Task ListHistoryByDecisionAsync_Should_ReturnImmutableProvenance_InDeterministicHistoricalOrder()
    {
        // Arrange
        DecisionId requestedDecisionId =
            DecisionId.New();

        DecisionId otherDecisionId =
            DecisionId.New();

        DecisionTargetReference target =
            DecisionTargetReference.ForParticipation(
                ParticipationId.New());

        DateTimeOffset olderAppliedAtUtc =
            new(
                2026,
                9,
                1,
                8,
                0,
                0,
                TimeSpan.Zero);

        DateTimeOffset newerAppliedAtUtc =
            new(
                2026,
                9,
                2,
                9,
                0,
                0,
                TimeSpan.Zero);

        DecisionApplication olderApplication =
            DecisionApplication.Create(
                requestedDecisionId,
                target,
                DecisionRevision.Initial,
                olderAppliedAtUtc);

        DecisionApplication sameTimeApplicationOne =
            DecisionApplication.Create(
                requestedDecisionId,
                target,
                DecisionRevision.From(2),
                newerAppliedAtUtc);

        DecisionApplication sameTimeApplicationTwo =
            DecisionApplication.Create(
                requestedDecisionId,
                target,
                DecisionRevision.From(3),
                newerAppliedAtUtc);

        DecisionApplication otherDecisionApplication =
            DecisionApplication.Create(
                otherDecisionId,
                target,
                DecisionRevision.From(7),
                new DateTimeOffset(
                    2026,
                    9,
                    3,
                    10,
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
                    sameTimeApplicationOne,
                    sameTimeApplicationTwo,
                    otherDecisionApplication);

            await seedContext.SaveChangesAsync();
        }

        await using ProjectAtmacaDbContext queryContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        IReadOnlyList<DecisionApplicationId>
            expectedSameTimeOrder =
                await queryContext
                    .Set<DecisionApplication>()
                    .AsNoTracking()
                    .Where(
                        application =>
                            application.DecisionId ==
                            requestedDecisionId &&
                            application.AppliedAtUtc ==
                            newerAppliedAtUtc)
                    .OrderByDescending(
                        application =>
                            application.Id)
                    .Select(
                        application =>
                            application.DecisionApplicationId)
                    .ToListAsync();

        var reader =
            new DecisionApplicationReader(
                queryContext);

        // Act
        IReadOnlyList<DecisionApplicationHistoryItem> result =
            await reader.ListHistoryByDecisionAsync(
                requestedDecisionId,
                CancellationToken.None);

        // Assert
        result
            .Should()
            .HaveCount(3);

        result
            .Select(item => item.DecisionApplicationId)
            .Should()
            .ContainInOrder(
                expectedSameTimeOrder[0],
                expectedSameTimeOrder[1],
                olderApplication.DecisionApplicationId);

        result
            .Select(
                item =>
                    item.DecisionId)
            .Should()
            .OnlyContain(
                decisionId =>
                    decisionId ==
                    requestedDecisionId);

        result
            .Single(
                item =>
                    item.DecisionApplicationId ==
                    sameTimeApplicationOne.DecisionApplicationId)
            .AppliedDecisionRevision
            .Should()
            .Be(
                sameTimeApplicationOne.AppliedDecisionRevision);

        result
            .Single(
                item =>
                    item.DecisionApplicationId ==
                    sameTimeApplicationTwo.DecisionApplicationId)
            .AppliedDecisionRevision
            .Should()
            .Be(
                sameTimeApplicationTwo.AppliedDecisionRevision);

        result
            .Single(
                item =>
                    item.DecisionApplicationId ==
                    olderApplication.DecisionApplicationId)
            .AppliedDecisionRevision
            .Should()
            .Be(
                DecisionRevision.Initial);

        result[0].AppliedAtUtc
            .Should()
            .Be(newerAppliedAtUtc);

        result[1].AppliedAtUtc
            .Should()
            .Be(newerAppliedAtUtc);

        result[2].AppliedAtUtc
            .Should()
            .Be(olderAppliedAtUtc);

        result
            .Select(
                item =>
                    item.Target)
            .Should()
            .OnlyContain(
                itemTarget =>
                    itemTarget ==
                    target);
    }
}
