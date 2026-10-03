using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubOrganizationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM [Organizations]
                    WHERE [Id] = 'f6c1a6a1-4f2c-4d0f-bb9b-202620270020'
                )
                BEGIN
                    INSERT INTO [Organizations]
                        ([Id], [ParentOrganizationId], [Name], [Code], [Description], [IsActive], [CreatedAtUtc])
                    VALUES
                        ('f6c1a6a1-4f2c-4d0f-bb9b-202620270020', NULL, N'Çaykur Rizespor', 'CR', N'Kulüp üst kurumu', 1, SYSUTCDATETIME());
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM [Organizations]
                    WHERE [Id] = 'f6c1a6a1-4f2c-4d0f-bb9b-202620270002'
                )
                    THROW 51000, 'Çaykur Rizespor Futbol Akademisi organization was not found.', 1;

                UPDATE [Organizations]
                SET [ParentOrganizationId] = 'f6c1a6a1-4f2c-4d0f-bb9b-202620270020'
                WHERE [Id] = 'f6c1a6a1-4f2c-4d0f-bb9b-202620270002';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ParentOrganizationId",
                table: "Organizations",
                column: "ParentOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Organizations_ParentOrganizationId",
                table: "Organizations",
                column: "ParentOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Organizations]
                SET [ParentOrganizationId] = NULL
                WHERE [Id] = 'f6c1a6a1-4f2c-4d0f-bb9b-202620270002'
                  AND [ParentOrganizationId] = 'f6c1a6a1-4f2c-4d0f-bb9b-202620270020';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Organizations_ParentOrganizationId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_ParentOrganizationId",
                table: "Organizations");
        }
    }
}
