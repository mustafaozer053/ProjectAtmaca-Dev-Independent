using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSportSpecificPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SportName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AtmacaCardSportsProfilePositions",
                columns: table => new
                {
                    AtmacaCardSportsProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtmacaCardSportsProfilePositions", x => new { x.AtmacaCardSportsProfileId, x.PositionId });
                    table.ForeignKey(
                        name: "FK_AtmacaCardSportsProfilePositions_AtmacaCardSportsProfiles_AtmacaCardSportsProfileId",
                        column: x => x.AtmacaCardSportsProfileId,
                        principalTable: "AtmacaCardSportsProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtmacaCardSportsProfilePositions_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtmacaCardSportsProfilePositions_PositionId",
                table: "AtmacaCardSportsProfilePositions",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SportName_Code",
                table: "Positions",
                columns: new[] { "SportName", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtmacaCardSportsProfilePositions");

            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
