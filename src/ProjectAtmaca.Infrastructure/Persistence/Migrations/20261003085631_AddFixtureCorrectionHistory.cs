using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixtureCorrectionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixtureCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ReopenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReopenedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FixtureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixtureCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixtureCorrections_Fixtures_FixtureId",
                        column: x => x.FixtureId,
                        principalTable: "Fixtures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixtureCorrections_FixtureId_ReopenedAtUtc",
                table: "FixtureCorrections",
                columns: new[] { "FixtureId", "ReopenedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixtureCorrections");
        }
    }
}
