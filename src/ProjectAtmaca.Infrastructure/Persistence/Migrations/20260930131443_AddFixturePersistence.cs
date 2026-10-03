using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixturePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fixtures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Opponent = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VenueSide = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fixtures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fixtures_SeasonTeams_SeasonTeamId",
                        column: x => x.SeasonTeamId,
                        principalTable: "SeasonTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fixtures_SeasonTeamId_Date_StartTime",
                table: "Fixtures",
                columns: new[] { "SeasonTeamId", "Date", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fixtures");
        }
    }
}
