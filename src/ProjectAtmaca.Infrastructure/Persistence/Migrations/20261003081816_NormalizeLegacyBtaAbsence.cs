using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyBtaAbsence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Participations]
                SET [ConditionCode] = NULL
                WHERE [ConditionCode] = 'BTA'
                  AND [Status] = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This data normalization is intentionally irreversible.
        }
    }
}
