using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonTeamMembershipAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeasonTeamMembershipAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SeasonTeamMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonTeamMembershipAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonTeamMembershipAssignments_SeasonTeamMemberships_SeasonTeamMembershipId",
                        column: x => x.SeasonTeamMembershipId,
                        principalTable: "SeasonTeamMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTeamMembershipAssignments_SeasonTeamMembershipId_Kind_DefinitionId",
                table: "SeasonTeamMembershipAssignments",
                columns: new[] { "SeasonTeamMembershipId", "Kind", "DefinitionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeasonTeamMembershipAssignments");
        }
    }
}
