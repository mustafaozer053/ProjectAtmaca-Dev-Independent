using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceRegistrationNationalIdentityUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM PersonRegistrations CROSS APPLY OPENJSON([Identity]) j
                    WHERE j.[key] = 'NationalId' AND j.[type] <> 0 AND
                    (j.[type] <> 1 OR DATALENGTH(j.[value]) <> 22 OR j.[value] COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%'))
                    THROW 51004, 'Existing national identity format requires correction before migration.', 1;
                """);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdentityNumber",
                table: "PersonRegistrations",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.Sql("UPDATE [PersonRegistrations] SET [NationalIdentityNumber] = JSON_VALUE([Identity], '$.NationalId');");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRegistrations_NationalIdentityNumber",
                table: "PersonRegistrations",
                column: "NationalIdentityNumber",
                unique: true,
                filter: "[NationalIdentityNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonRegistrations_NationalIdentityNumber",
                table: "PersonRegistrations");

            migrationBuilder.DropColumn(
                name: "NationalIdentityNumber",
                table: "PersonRegistrations");
        }
    }
}
