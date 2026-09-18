using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectAtmaca.Infrastructure.Persistence;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

public sealed class ParticipationHistoryIndexMigrationTests
{
    [Fact]
    public async Task Migration_Should_CreateCoveringHistoryIndex_AndRollbackWithoutRemovingUniqueIndex()
    {
        string database = $"ProjectAtmaca_HistoryIndex_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        await using var context = new ProjectAtmacaDbContext(options);
        try
        {
            await context.Database.MigrateAsync();
            await context.Database.OpenConnectionAsync();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT c.name, ic.key_ordinal, ic.is_descending_key, ic.is_included_column, i.is_unique
                FROM sys.indexes i
                JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE i.object_id = OBJECT_ID(N'dbo.Participations')
                  AND i.name = N'IX_Participations_AtmacaCard_CreatedAt_Id'
                ORDER BY ic.is_included_column, ic.key_ordinal, c.name
                """;
            var keys = new List<(string, bool)>();
            var includes = new List<string>();
            await using (var rows = await command.ExecuteReaderAsync())
            {
                while (await rows.ReadAsync())
                {
                    rows.GetBoolean(4).Should().BeFalse();
                    if (rows.GetBoolean(3)) includes.Add(rows.GetString(0));
                    else keys.Add((rows.GetString(0), rows.GetBoolean(2)));
                }
            }
            keys.Should().Equal(("AtmacaCardId", false), ("CreatedAtUtc", true), ("Id", true));
            includes.Should().BeEquivalentTo("ActivityReference", "Status", "ConditionCode", "JoinedAt", "LeftAt");
            context.Database.HasPendingModelChanges().Should().BeFalse();
            await context.GetService<IMigrator>().MigrateAsync("20260907113013_AddActorPermissionGrantPersistence");
            command.CommandText = """
                SELECT COUNT(*) FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.Participations')
                  AND name = N'IX_Participations_AtmacaCard_CreatedAt_Id'
                """;
            Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(0);
            command.CommandText = """
                SELECT COUNT(*) FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.Participations')
                  AND name = N'UX_Participations_AtmacaCard_Activity' AND is_unique = 1
                """;
            Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(1);
        }
        finally
        {
            context.Database.GetDbConnection().Database.Should().Be(database);
            await context.Database.EnsureDeletedAsync();
        }
    }
}
