using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using Xunit.Abstractions;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

public sealed class ParticipationHistoryQueryCostTests(ITestOutputHelper output)
{
    private static async Task SetStatisticsAsync(SqlConnection connection, bool enabled)
    {
        using var command = connection.CreateCommand();
        command.CommandText = enabled ? "SET STATISTICS IO ON" : "SET STATISTICS IO OFF";
        await command.ExecuteNonQueryAsync();
    }
    // Synthetic history sizes, not athlete counts or supported-capacity guarantees.
    [Theory]
    [InlineData(30, 0)]
    [InlineData(300, 0)]
    [InlineData(3000, 0)]
    [InlineData(3000, 3000)]
    public async Task Reader_Should_PreservePages_WhenCandidateIndexIsMeasured(
        int historyCount, int unrelatedCount)
    {
        string database = $"ProjectAtmaca_QueryCost_{Guid.NewGuid():N}";
        var capture = new SqlQueryMeasurement();
        var options = new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
            .AddInterceptors(capture)
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True")
            .LogTo(message => output.WriteLine(message),
                new[] { DbLoggerCategory.Database.Command.Name })
            .Options;
        await using var context = new ProjectAtmacaDbContext(options);
        var card = AtmacaCardId.New();
        var other = AtmacaCardId.New();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        try
        {
            // Freeze the pre-index baseline even after the production migration is added.
            await context.GetService<IMigrator>().MigrateAsync("20260907113013_AddActorPermissionGrantPersistence");
            for (int i = 0; i < historyCount + unrelatedCount; i++)
            {
                var item = Participation.Create(
                    ActivityReference.ForTraining(TrainingId.New()),
                    i < historyCount ? card : other).Value!;
                context.Participations.Add(item);
                context.Entry(item).Property(x => x.CreatedAtUtc).CurrentValue = start.AddMinutes(i);
            }
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            await context.Database.OpenConnectionAsync();
            var connection = (SqlConnection)context.Database.GetDbConnection();
            connection.InfoMessage += (_, args) => output.WriteLine(args.Message);
            var reader = new ParticipationReader(context);
            // Warm both query shapes; elapsed time is deliberately not an assertion.
            var first = await reader.ListHistoryByAtmacaCardAsync(card, 20, null);
            var second = await reader.ListHistoryByAtmacaCardAsync(card, 20, first.NextCursor);
            await SetStatisticsAsync(connection, true);
            output.WriteLine($"BASELINE history={historyCount} unrelated={unrelatedCount}");
            var baselineFirst = await reader.ListHistoryByAtmacaCardAsync(card, 20, null);
            await capture.MeasureAsync(output);
            var baselineSecond = await reader.ListHistoryByAtmacaCardAsync(card, 20, first.NextCursor);
            await capture.MeasureAsync(output);
            await SetStatisticsAsync(connection, false);
            // Experimental DDL exists only in this unique, disposable test database.
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IX_QueryCost_Candidate ON dbo.Participations (AtmacaCardId, CreatedAtUtc DESC, Id DESC)");
            await reader.ListHistoryByAtmacaCardAsync(card, 20, null);
            await reader.ListHistoryByAtmacaCardAsync(card, 20, first.NextCursor);
            await SetStatisticsAsync(connection, true);
            output.WriteLine($"CANDIDATE history={historyCount} unrelated={unrelatedCount}");
            var indexedFirst = await reader.ListHistoryByAtmacaCardAsync(card, 20, null);
            await capture.MeasureAsync(output);
            var indexedSecond = await reader.ListHistoryByAtmacaCardAsync(card, 20, first.NextCursor);
            await capture.MeasureAsync(output);
            await SetStatisticsAsync(connection, false);
            // Replace the narrow candidate so each alternative is measured independently.
            await context.Database.ExecuteSqlRawAsync("DROP INDEX IX_QueryCost_Candidate ON dbo.Participations");
            await context.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IX_QueryCost_Covering ON dbo.Participations (AtmacaCardId, CreatedAtUtc DESC, Id DESC) INCLUDE (ActivityReference, Status, ConditionCode, JoinedAt, LeftAt)");
            await reader.ListHistoryByAtmacaCardAsync(card, 20, null);
            await reader.ListHistoryByAtmacaCardAsync(card, 20, first.NextCursor);
            output.WriteLine($"COVERING history={historyCount} unrelated={unrelatedCount}");
            var coveringFirst = await reader.ListHistoryByAtmacaCardAsync(card, 20, null);
            await capture.MeasureAsync(output);
            var coveringSecond = await reader.ListHistoryByAtmacaCardAsync(card, 20, first.NextCursor);
            await capture.MeasureAsync(output);
            coveringFirst.Should().BeEquivalentTo(baselineFirst, o => o.WithStrictOrdering());
            coveringSecond.Should().BeEquivalentTo(baselineSecond, o => o.WithStrictOrdering());
            indexedFirst.Should().BeEquivalentTo(baselineFirst, o => o.WithStrictOrdering());
            indexedSecond.Should().BeEquivalentTo(baselineSecond, o => o.WithStrictOrdering());
            baselineFirst.Should().BeEquivalentTo(first, o => o.WithStrictOrdering());
            baselineSecond.Should().BeEquivalentTo(second, o => o.WithStrictOrdering());
            first.Items.Should().HaveCount(20);
            second.Items.Should().HaveCount(Math.Min(20, historyCount - 20));
        }
        finally
        {
            capture.Query?.Dispose();
            context.Database.GetDbConnection().Database.Should().Be(database);
            await context.Database.EnsureDeletedAsync();
        }
    }
}
