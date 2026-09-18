using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionHistoryCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DecisionApplications_Decision_AppliedAt_Id",
                table: "DecisionApplications",
                columns: new[] { "DecisionId", "AppliedAtUtc", "Id" },
                descending: new[] { false, true, true })
                .Annotation("SqlServer:Include", new[] { "AppliedDecisionRevision", "TargetType", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DecisionApplications_Decision_AppliedAt_Id",
                table: "DecisionApplications");
        }
    }
}
