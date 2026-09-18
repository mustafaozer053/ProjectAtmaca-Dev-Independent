using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportDuplicateLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PassportMatchKey",
                table: "PersonRegistrations",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [PersonRegistrations]
                SET [PassportMatchKey] = CONVERT(varchar(64), HASHBYTES('SHA2_256', JSON_VALUE([Identity], '$.Passport')), 2);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PersonRegistrations_PassportMatchKey",
                table: "PersonRegistrations",
                column: "PassportMatchKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PersonRegistrations_PassportMatchKey",
                table: "PersonRegistrations");

            migrationBuilder.DropColumn(
                name: "PassportMatchKey",
                table: "PersonRegistrations");
        }
    }
}
