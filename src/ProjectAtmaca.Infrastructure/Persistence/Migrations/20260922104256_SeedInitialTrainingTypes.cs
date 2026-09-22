using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialTrainingTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO TrainingTypes (Id, Code, Name, Description, DisplayOrder, IsActive, CreatedAtUtc)
                VALUES
                ('f6c1a6a1-4f2c-4d0f-bb9b-202630000001', 'TECHNICAL', N'Teknik çalışma', N'Teknik beceri ve top çalışmaları', 10, 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202630000002', 'TACTICAL', N'Taktik çalışma', N'Taktik organizasyon ve oyun planı', 20, 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202630000003', 'CONDITIONING', N'Kondisyon', N'Fiziksel hazırlık ve dayanıklılık', 30, 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202630000004', 'MATCH', N'Maç', N'Hazırlık veya resmi maç çalışması', 40, 1, SYSUTCDATETIME());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM TrainingTypes
                WHERE Id IN (
                    'f6c1a6a1-4f2c-4d0f-bb9b-202630000001',
                    'f6c1a6a1-4f2c-4d0f-bb9b-202630000002',
                    'f6c1a6a1-4f2c-4d0f-bb9b-202630000003',
                    'f6c1a6a1-4f2c-4d0f-bb9b-202630000004');
                """);
        }
    }
}
