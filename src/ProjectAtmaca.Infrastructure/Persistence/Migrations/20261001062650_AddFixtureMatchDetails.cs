using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixtureMatchDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Fixtures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchNotes",
                table: "Fixtures",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referee",
                table: "Fixtures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FixtureMatchEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Minute = table.Column<int>(type: "int", nullable: false),
                    AtmacaCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedAtmacaCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FixtureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixtureMatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixtureMatchEvents_Fixtures_FixtureId",
                        column: x => x.FixtureId,
                        principalTable: "Fixtures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FixtureSquadMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtmacaCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    FixtureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixtureSquadMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixtureSquadMembers_Fixtures_FixtureId",
                        column: x => x.FixtureId,
                        principalTable: "Fixtures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixtureMatchEvents_FixtureId_Minute",
                table: "FixtureMatchEvents",
                columns: new[] { "FixtureId", "Minute" });

            migrationBuilder.CreateIndex(
                name: "IX_FixtureSquadMembers_FixtureId_AtmacaCardId",
                table: "FixtureSquadMembers",
                columns: new[] { "FixtureId", "AtmacaCardId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixtureMatchEvents");

            migrationBuilder.DropTable(
                name: "FixtureSquadMembers");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Fixtures");

            migrationBuilder.DropColumn(
                name: "MatchNotes",
                table: "Fixtures");

            migrationBuilder.DropColumn(
                name: "Referee",
                table: "Fixtures");
        }
    }
}
