using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAtmacaCardSportsProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AtmacaCardSportsProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtmacaCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SportName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedSportOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ClubRegisteredOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CompetitionLevel = table.Column<int>(type: "int", nullable: true),
                    IsNationalAthlete = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtmacaCardSportsProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtmacaCardSportsProfiles_AtmacaCards_AtmacaCardId",
                        column: x => x.AtmacaCardId,
                        principalTable: "AtmacaCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtmacaCardSportsProfiles_AtmacaCardId_SportName",
                table: "AtmacaCardSportsProfiles",
                columns: new[] { "AtmacaCardId", "SportName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtmacaCardSportsProfiles");
        }
    }
}
