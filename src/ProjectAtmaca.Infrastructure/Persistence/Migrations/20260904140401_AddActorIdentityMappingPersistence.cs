using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActorIdentityMappingPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActorIdentityMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Issuer = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuerByteLength = table.Column<int>(type: "int", nullable: false, computedColumnSql: "DATALENGTH([Issuer])", stored: true),
                    SubjectByteLength = table.Column<int>(type: "int", nullable: false, computedColumnSql: "DATALENGTH([Subject])", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorIdentityMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActorIdentityMappings_ActorId",
                table: "ActorIdentityMappings",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActorIdentityMappings_Issuer_IssuerByteLength_Subject_SubjectByteLength",
                table: "ActorIdentityMappings",
                columns: new[] { "Issuer", "IssuerByteLength", "Subject", "SubjectByteLength" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActorIdentityMappings");
        }
    }
}
