using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using Xunit.Abstractions;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

public sealed class ParticipationActivityQueryCostTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(30, 0)]
    [InlineData(30, 3000)]
    [InlineData(300, 3000)]
    [InlineData(3000, 3000)]
    public async Task Reader_Should_PreserveActivityListAndSummary_WhenIndexesAreCompared(
        int participants, int unrelated)
    {
        string database = $"ProjectAtmaca_ActivityCost_{Guid.NewGuid():N}";
        var capture = new SqlQueryMeasurement();
        var options = new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True")
            .AddInterceptors(capture).Options;
        await using var context = new ProjectAtmacaDbContext(options);
        var target = ActivityReference.ForTraining(TrainingId.New());
        var other = ActivityReference.ForTraining(TrainingId.New());
        var expected = new List<Guid>();
        try
        {
            // Includes the production history index, freezes the comparison baseline.
            await context.GetService<IMigrator>().MigrateAsync("20260914124620_AddParticipationHistoryCoveringIndex");
            for (int i = 0; i < participants + unrelated; i++)
            {
                var item = Participation.Create(i < participants ? target : other, AtmacaCardId.New()).Value!;
                if (i < participants) expected.Add(item.Id);
                context.Participations.Add(item);
            }
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            await context.Database.OpenConnectionAsync();
            var connection = (SqlConnection)context.Database.GetDbConnection();
            connection.InfoMessage += (_, args) => output.WriteLine(args.Message);
            var reader = new ParticipationReader(context);
            var baseline = await reader.ListByActivityAsync(target);
            var summary = await reader.GetSummaryByActivityAsync(target);
            baseline.Select(x => x.Id).Should().Equal(expected.OrderBy(x => x));
            summary.Total.Should().Be(participants);
            summary.NotRecorded.Should().Be(participants);

            foreach (string phase in new[] { "BASELINE", "NARROW", "COVERING" })
            {
                if (phase == "NARROW")
                    await context.Database.ExecuteSqlRawAsync(
                        "CREATE INDEX IX_ActivityCost_Narrow ON dbo.Participations (ActivityReference)");
                if (phase == "COVERING")
                {
                    await context.Database.ExecuteSqlRawAsync("DROP INDEX IX_ActivityCost_Narrow ON dbo.Participations");
                    await context.Database.ExecuteSqlRawAsync(
                        "CREATE INDEX IX_ActivityCost_Covering ON dbo.Participations (ActivityReference) INCLUDE (AtmacaCardId, Status, ConditionCode, JoinedAt, LeftAt)");
                }
                // Id is the clustered key and is implicitly available in either index.
                await reader.ListByActivityAsync(target);
                await reader.GetSummaryByActivityAsync(target);
                output.WriteLine($"CASE {phase} participants={participants} unrelated={unrelated} LIST");
                var list = await reader.ListByActivityAsync(target);
                await capture.MeasureAsync(output);
                list.Should().BeEquivalentTo(baseline, o => o.WithStrictOrdering());
                output.WriteLine($"CASE {phase} participants={participants} unrelated={unrelated} SUMMARY");
                var actualSummary = await reader.GetSummaryByActivityAsync(target);
                await capture.MeasureAsync(output);
                actualSummary.Should().BeEquivalentTo(summary);
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
