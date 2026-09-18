using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Services;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class SqlAtmacaCardNumberGenerator(ProjectAtmacaDbContext context) : IAtmacaCardNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Do not compose this query: SQL Server disallows NEXT VALUE FOR in subqueries.
            var values = await context.Database.SqlQueryRaw<long>(
                "SELECT NEXT VALUE FOR [dbo].[AtmacaCardNumbers] AS [Value]")
                .ToListAsync(cancellationToken);
            return "ATM-" + values.Single().ToString("D6", CultureInfo.InvariantCulture);
        }
        catch (SqlException exception) when (exception.Number == 11728)
        {
            throw new AtmacaCardNumberCapacityException(exception);
        }
    }
}
