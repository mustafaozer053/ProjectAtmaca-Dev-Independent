using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;
using System.Data.SqlTypes;
using Xunit.Abstractions;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Decisions;

public sealed class DecisionHistoryQueryCostTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(30, 0)]
    [InlineData(30, 3000)]
    [InlineData(300, 3000)]
    [InlineData(3000, 3000)]
    public async Task Reader_Should_PreserveDecisionHistory_WhenIndexesAreCompared(int history, int unrelated)
    {
        string database = $"ProjectAtmaca_DecisionCost_{Guid.NewGuid():N}";
        var capture = new SqlQueryMeasurement();
        var options = new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True")
            .AddInterceptors(capture).Options;
        await using var context = new ProjectAtmacaDbContext(options);
        var decision = DecisionId.New();
        var other = DecisionId.New();
        var expected = new List<DecisionApplication>();
        var time = new DateTimeOffset(2026, 9, 14, 0, 0, 0, TimeSpan.Zero);
        try
        {
            await context.GetService<IMigrator>().MigrateAsync("20260914125625_AddParticipationActivityCoveringIndex");
            for (int i = 0; i < history + unrelated; i++)
            {
                var item = DecisionApplication.Create(i < history ? decision : other,
                    DecisionTargetReference.ForParticipation(ParticipationId.New()),
                    DecisionRevision.From(i % 3 + 1), time.AddMinutes(i / 3));
                context.Set<DecisionApplication>().Add(item);
                // The first two equal-time IDs sort differently under SQL and .NET.
                if (i < 2)
                    context.Entry(item).Property(x => x.Id).CurrentValue = Guid.Parse(i == 0
                        ? "00000000-0000-0000-0000-000000000002"
                        : "ffffffff-ffff-ffff-ffff-000000000001");
                if (i < history) expected.Add(item);
            }
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            await context.Database.OpenConnectionAsync();
            var connection = (SqlConnection)context.Database.GetDbConnection();
            connection.InfoMessage += (_, args) => output.WriteLine(args.Message);
            var reader = new DecisionApplicationReader(context);
            var baseline = await reader.ListHistoryByDecisionAsync(decision);
            baseline.Select(x => x.DecisionApplicationId.Value).Should().Equal(expected
                .OrderByDescending(x => x.AppliedAtUtc).ThenByDescending(x => new SqlGuid(x.Id))
                .Select(x => x.Id));
            baseline.Should().OnlyContain(x => x.DecisionId == decision);
            foreach (string phase in new[] { "BASELINE", "NARROW", "COVERING" })
            {
                if (phase == "NARROW")
                    await context.Database.ExecuteSqlRawAsync(
                        "CREATE INDEX IX_DecisionCost_Narrow ON dbo.DecisionApplications (DecisionId, AppliedAtUtc DESC, Id DESC)");
                if (phase == "COVERING")
                {
                    await context.Database.ExecuteSqlRawAsync("DROP INDEX IX_DecisionCost_Narrow ON dbo.DecisionApplications");
                    await context.Database.ExecuteSqlRawAsync(
                        "CREATE INDEX IX_DecisionCost_Covering ON dbo.DecisionApplications (DecisionId, AppliedAtUtc DESC, Id DESC) INCLUDE (AppliedDecisionRevision, TargetType, TargetId)");
                }
                await reader.ListHistoryByDecisionAsync(decision);
                output.WriteLine($"CASE {phase} history={history} unrelated={unrelated}");
                var actual = await reader.ListHistoryByDecisionAsync(decision);
                await capture.MeasureAsync(output);
                actual.Should().BeEquivalentTo(baseline, o => o.WithStrictOrdering());
            }
        }
        finally
        {
            capture.Query?.Dispose();
            context.Database.GetDbConnection().Database.Should().Be(database);
            await context.Database.EnsureDeletedAsync();
        }
    }
}
