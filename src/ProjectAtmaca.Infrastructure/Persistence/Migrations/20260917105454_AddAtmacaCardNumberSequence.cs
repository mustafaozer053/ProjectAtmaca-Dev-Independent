using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAtmacaCardNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence(
                name: "AtmacaCardNumbers",
                schema: "dbo",
                minValue: 1L,
                maxValue: 999999L);
            // Continue after existing cards when upgrading an already populated database.
            migrationBuilder.Sql("""
                DECLARE @next bigint = (SELECT ISNULL(MAX(TRY_CONVERT(bigint, SUBSTRING([CardNumber], 5, 6))), 0) + 1 FROM [AtmacaCards]);
                IF @next > 999999 THROW 51001, 'Atmaca card number capacity is exhausted.', 1;
                DECLARE @sql nvarchar(200) = N'ALTER SEQUENCE [dbo].[AtmacaCardNumbers] RESTART WITH ' + CONVERT(nvarchar(20), @next);
                EXEC sys.sp_executesql @sql;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "AtmacaCardNumbers",
                schema: "dbo");
        }
    }
}
