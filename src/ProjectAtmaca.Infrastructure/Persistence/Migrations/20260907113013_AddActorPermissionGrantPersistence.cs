using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActorPermissionGrantPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActorPermissionGrants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    PermissionCodeByteLength = table.Column<int>(type: "int", nullable: false, computedColumnSql: "DATALENGTH([PermissionCode])", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorPermissionGrants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActorPermissionGrants_ActorId_PermissionCode_PermissionCodeByteLength",
                table: "ActorPermissionGrants",
                columns: new[] { "ActorId", "PermissionCode", "PermissionCodeByteLength" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActorPermissionGrants");
        }
    }
}
