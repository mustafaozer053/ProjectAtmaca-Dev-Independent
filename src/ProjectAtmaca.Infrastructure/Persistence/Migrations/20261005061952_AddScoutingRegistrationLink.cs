using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutingRegistrationLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RegisteredAtmacaCardId",
                table: "ScoutingCandidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredCardNumber",
                table: "ScoutingCandidates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegisteredPersonId",
                table: "ScoutingCandidates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingCandidates_RegisteredPersonId",
                table: "ScoutingCandidates",
                column: "RegisteredPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScoutingCandidates_RegisteredPersonId",
                table: "ScoutingCandidates");

            migrationBuilder.DropColumn(
                name: "RegisteredAtmacaCardId",
                table: "ScoutingCandidates");

            migrationBuilder.DropColumn(
                name: "RegisteredCardNumber",
                table: "ScoutingCandidates");

            migrationBuilder.DropColumn(
                name: "RegisteredPersonId",
                table: "ScoutingCandidates");
        }
    }
}
