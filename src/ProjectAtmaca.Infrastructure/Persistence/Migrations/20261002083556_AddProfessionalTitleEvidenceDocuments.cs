using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalTitleEvidenceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonProfessionalTitleEvidenceDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonProfessionalTitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtmacaCardDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonProfessionalTitleEvidenceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonProfessionalTitleEvidenceDocuments_AtmacaCardDocuments_AtmacaCardDocumentId",
                        column: x => x.AtmacaCardDocumentId,
                        principalTable: "AtmacaCardDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonProfessionalTitleEvidenceDocuments_PersonProfessionalTitles_PersonProfessionalTitleId",
                        column: x => x.PersonProfessionalTitleId,
                        principalTable: "PersonProfessionalTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonProfessionalTitleEvidenceDocuments_AtmacaCardDocumentId",
                table: "PersonProfessionalTitleEvidenceDocuments",
                column: "AtmacaCardDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonProfessionalTitleEvidenceDocuments_PersonProfessionalTitleId_AtmacaCardDocumentId",
                table: "PersonProfessionalTitleEvidenceDocuments",
                columns: new[] { "PersonProfessionalTitleId", "AtmacaCardDocumentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonProfessionalTitleEvidenceDocuments");
        }
    }
}
