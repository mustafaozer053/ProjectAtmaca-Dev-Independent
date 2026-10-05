using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutingIdentityAndPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PositionIds",
                table: "ScoutingObservations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdentityKey",
                table: "ScoutingCandidates",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                table: "ScoutingCandidates",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingCandidates_IdentityKey",
                table: "ScoutingCandidates",
                column: "IdentityKey",
                unique: true,
                filter: "[IdentityKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScoutingCandidates_IdentityKey",
                table: "ScoutingCandidates");

            migrationBuilder.DropColumn(
                name: "PositionIds",
                table: "ScoutingObservations");

            migrationBuilder.DropColumn(
                name: "IdentityKey",
                table: "ScoutingCandidates");

            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                table: "ScoutingCandidates");
        }
    }
}
