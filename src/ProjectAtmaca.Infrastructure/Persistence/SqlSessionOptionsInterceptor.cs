using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ProjectAtmaca.Infrastructure.Persistence;

public sealed class SqlSessionOptionsInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        SetNumericRoundAbortOff(connection);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetNumericRoundAbortOffAsync(connection, cancellationToken);
    }

    private static void SetNumericRoundAbortOff(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SET NUMERIC_ROUNDABORT OFF;";
        command.ExecuteNonQuery();
    }

    private static async Task SetNumericRoundAbortOffAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SET NUMERIC_ROUNDABORT OFF;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
