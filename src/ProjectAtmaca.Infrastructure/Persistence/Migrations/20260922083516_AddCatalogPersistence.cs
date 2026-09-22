using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgeGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO Seasons (Id, Name, Period, CreatedAtUtc)
                VALUES ('f6c1a6a1-4f2c-4d0f-bb9b-202620270001', '2026-2027', '2026-07-01|2027-06-30', SYSUTCDATETIME());
                """);

            migrationBuilder.Sql("""
                INSERT INTO Organizations (Id, ParentOrganizationId, Name, Code, Description, IsActive, CreatedAtUtc)
                VALUES ('f6c1a6a1-4f2c-4d0f-bb9b-202620270002', NULL, N'Çaykur Rizespor Futbol Akademisi', 'CRFA', NULL, 1, SYSUTCDATETIME());
                """);

            migrationBuilder.Sql("""
                INSERT INTO AgeGroups (Id, Code, IsActive, CreatedAtUtc)
                VALUES
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270019', 'U19', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270017', 'U17', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270016', 'U16', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270015', 'U15', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270014', 'U14', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270013', 'U13', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270012', 'U12', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270011', 'U11', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270010', 'U10', 1, SYSUTCDATETIME()),
                ('f6c1a6a1-4f2c-4d0f-bb9b-202620270009', 'U9', 1, SYSUTCDATETIME());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgeGroups");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Seasons");
        }
    }
}
