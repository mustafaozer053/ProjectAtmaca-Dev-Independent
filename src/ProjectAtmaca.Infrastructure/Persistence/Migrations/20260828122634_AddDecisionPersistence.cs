using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecisionApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliedDecisionRevision = table.Column<int>(type: "int", nullable: false),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    EffectOutcome = table.Column<int>(type: "int", nullable: false),
                    SupersededByDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshotActivityReference = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SnapshotAtmacaCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotConditionCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    SnapshotJoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SnapshotLeftAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SnapshotStatus = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionApplications");

            migrationBuilder.DropTable(
                name: "Decisions");
        }
    }
}
