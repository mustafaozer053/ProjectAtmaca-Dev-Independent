using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Readers;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Decisions;

public sealed class DecisionHistoryIndexMigrationTests
{
    [Fact]
    public async Task Migration_Should_CoverComplexTargetColumns_AndPreserveHistoryThroughRollbackAndReapply()
    {
        string database = $"ProjectAtmaca_DecisionIndex_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        await using var context = new ProjectAtmacaDbContext(options);
        try
        {
            var migrator = context.GetService<IMigrator>();
            const string previous = "20260914125625_AddParticipationActivityCoveringIndex";
            await migrator.MigrateAsync(previous);
            var decision = DecisionId.New();
            var application = DecisionApplication.Create(decision,
                DecisionTargetReference.ForParticipation(ParticipationId.New()), DecisionRevision.Initial,
                new DateTimeOffset(2026, 9, 14, 0, 0, 0, TimeSpan.Zero));
            context.Set<DecisionApplication>().Add(application);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var reader = new DecisionApplicationReader(context);
            var original = await reader.ListHistoryByDecisionAsync(decision);
            await context.Database.MigrateAsync();
            await context.Database.OpenConnectionAsync();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT c.name, ic.key_ordinal, ic.is_descending_key, ic.is_included_column, i.is_unique
                FROM sys.indexes i
                JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                WHERE i.object_id=OBJECT_ID(N'dbo.DecisionApplications')
                  AND i.name=N'IX_DecisionApplications_Decision_AppliedAt_Id'
                ORDER BY ic.is_included_column, ic.key_ordinal, c.name
                """;
            context.Database.HasPendingModelChanges().Should().BeFalse();
            string inspect = command.CommandText;
            for (int pass = 0; pass < 2; pass++)
            {
                command.CommandText = inspect;
                var keys = new List<(string, bool)>();
                var includes = new List<string>();
                await using (var rows = await command.ExecuteReaderAsync())
                    while (await rows.ReadAsync())
                    {
                        rows.GetBoolean(4).Should().BeFalse();
                        if (rows.GetBoolean(3)) includes.Add(rows.GetString(0));
                        else keys.Add((rows.GetString(0), rows.GetBoolean(2)));
                    }
                keys.Should().Equal(("DecisionId", false), ("AppliedAtUtc", true), ("Id", true));
                includes.Should().BeEquivalentTo("AppliedDecisionRevision", "TargetType", "TargetId");
                (await reader.ListHistoryByDecisionAsync(decision)).Should().BeEquivalentTo(original);
                if (pass == 0)
                {
                    await migrator.MigrateAsync(previous);
                    command.CommandText = "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DecisionApplications') AND name=N'IX_DecisionApplications_Decision_AppliedAt_Id'";
                    Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(0);
                    (await reader.ListHistoryByDecisionAsync(decision)).Should().BeEquivalentTo(original);
                    await context.Database.MigrateAsync();
                }
            }
        }
        finally
        {
            context.Database.GetDbConnection().Database.Should().Be(database);
            await context.Database.EnsureDeletedAsync();
        }
    }
}
