using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAtmacaCardMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AtmacaCardMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtmacaCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasuredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    HeightCentimeters = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WeightKilograms = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtmacaCardMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtmacaCardMeasurements_AtmacaCards_AtmacaCardId",
                        column: x => x.AtmacaCardId,
                        principalTable: "AtmacaCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtmacaCardMeasurements_AtmacaCardId_MeasuredOn",
                table: "AtmacaCardMeasurements",
                columns: new[] { "AtmacaCardId", "MeasuredOn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtmacaCardMeasurements");
        }
    }
}
