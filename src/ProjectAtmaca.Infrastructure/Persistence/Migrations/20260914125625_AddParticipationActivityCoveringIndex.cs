using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipationActivityCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Participations_ActivityReference",
                table: "Participations",
                column: "ActivityReference")
                .Annotation("SqlServer:Include", new[] { "AtmacaCardId", "Status", "ConditionCode", "JoinedAt", "LeftAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participations_ActivityReference",
                table: "Participations");
        }
    }
}
