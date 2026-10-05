using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutingCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoutingCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "date", nullable: true),
                    BirthPlace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryPhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecondaryPhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitialSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoutingDecision = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoutingCandidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScoutingObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ObservationType = table.Column<int>(type: "int", nullable: false),
                    ObservedEvent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ObservedClub = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ObservedTeam = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AgeGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DominantFoot = table.Column<int>(type: "int", nullable: true),
                    ObservedLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strengths = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Weaknesses = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ObserverRecommendation = table.Column<int>(type: "int", nullable: true),
                    RecommendationNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ObserverName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    CreatedByAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoutingCandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoutingObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoutingObservations_ScoutingCandidates_ScoutingCandidateId",
                        column: x => x.ScoutingCandidateId,
                        principalTable: "ScoutingCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingObservations_ScoutingCandidateId",
                table: "ScoutingObservations",
                column: "ScoutingCandidateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoutingObservations");

            migrationBuilder.DropTable(
                name: "ScoutingCandidates");
        }
    }
}
