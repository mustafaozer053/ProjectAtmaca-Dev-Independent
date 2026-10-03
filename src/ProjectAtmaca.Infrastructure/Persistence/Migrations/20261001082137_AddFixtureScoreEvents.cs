using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixtureScoreEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixtureScoreEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    ScoreTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScoreValue = table.Column<int>(type: "int", nullable: false),
                    Minute = table.Column<int>(type: "int", nullable: true),
                    AtmacaCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FixtureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixtureScoreEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixtureScoreEvents_Fixtures_FixtureId",
                        column: x => x.FixtureId,
                        principalTable: "Fixtures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixtureScoreEvents_AtmacaCardId",
                table: "FixtureScoreEvents",
                column: "AtmacaCardId");

            migrationBuilder.CreateIndex(
                name: "IX_FixtureScoreEvents_FixtureId_Side_ScoreTypeCode_Minute",
                table: "FixtureScoreEvents",
                columns: new[] { "FixtureId", "Side", "ScoreTypeCode", "Minute" });

            migrationBuilder.Sql(
                """
                INSERT INTO [FixtureScoreEvents]
                    ([Id], [Side], [ScoreTypeCode], [ScoreValue], [Minute],
                     [AtmacaCardId], [FixtureId])
                SELECT
                    [Id],
                    CASE [Type] WHEN 1 THEN 1 ELSE 2 END,
                    N'FOOTBALL_GOAL',
                    1,
                    [Minute],
                    NULL,
                    [FixtureId]
                FROM [FixtureMatchEvents]
                WHERE [Type] IN (1, 2);

                DELETE FROM [FixtureMatchEvents]
                WHERE [Type] IN (1, 2);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO [FixtureMatchEvents]
                    ([Id], [Type], [Minute], [AtmacaCardId],
                     [RelatedAtmacaCardId], [FixtureId])
                SELECT
                    NEWID(),
                    CASE [Side] WHEN 1 THEN 1 ELSE 2 END,
                    COALESCE([Minute], 0),
                    NULL,
                    NULL,
                    [FixtureId]
                FROM [FixtureScoreEvents]
                WHERE [ScoreTypeCode] = N'FOOTBALL_GOAL'
                  AND [ScoreValue] = 1;
                """);

            migrationBuilder.DropTable(
                name: "FixtureScoreEvents");
        }
    }
}
