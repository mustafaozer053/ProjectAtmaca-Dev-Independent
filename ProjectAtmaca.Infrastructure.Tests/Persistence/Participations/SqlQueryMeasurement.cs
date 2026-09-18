using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Xml.Linq;
using Xunit.Abstractions;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

internal sealed class SqlQueryMeasurement : DbCommandInterceptor
{
    public SqlCommand? Query { get; private set; }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.StartsWith("SELECT", StringComparison.Ordinal))
        {
            Query?.Dispose();
            Query = (SqlCommand)((ICloneable)command).Clone();
        }
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public async Task MeasureAsync(ITestOutputHelper output)
    {
        // Drain every result to receive SQL's trailing statistics messages.
        using var command = (SqlCommand)((ICloneable)Query!).Clone();
        command.CommandText = "SET STATISTICS IO ON; SET STATISTICS XML ON; " + command.CommandText;
        await using var rows = await command.ExecuteReaderAsync();
        int plans = 0;
        do
        {
            while (await rows.ReadAsync())
            {
                if (rows.FieldCount != 1 || !rows.GetName(0).Contains("Showplan"))
                    continue;
                string xml = rows.GetString(0);
                var plan = XDocument.Parse(xml);
                XNamespace ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";
                output.WriteLine("PLAN " + string.Join(" -> ", plan.Descendants(ns + "RelOp")
                    .Select(op => $"{op.Attribute("PhysicalOp")?.Value}/{op.Attribute("LogicalOp")?.Value}")));
                output.WriteLine("INDEXES " + string.Join(", ", plan.Descendants(ns + "Object")
                    .Select(obj => obj.Attribute("Index")?.Value).Where(index => index is not null)));
                output.WriteLine("SHOWPLAN_XML " + xml);
                plans++;
            }
        } while (await rows.NextResultAsync());
        plans.Should().Be(1, "each measured SQL query must return an actual execution plan");
    }
}
