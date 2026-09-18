using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipationHistoryCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Participations_AtmacaCard_CreatedAt_Id",
                table: "Participations",
                columns: new[] { "AtmacaCardId", "CreatedAtUtc", "Id" },
                descending: new[] { false, true, true })
                .Annotation("SqlServer:Include", new[] { "ActivityReference", "Status", "ConditionCode", "JoinedAt", "LeftAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participations_AtmacaCard_CreatedAt_Id",
                table: "Participations");
        }
    }
}
