using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonProfessionalTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonProfessionalTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StartedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonProfessionalTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonProfessionalTitles_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonProfessionalTitles_PersonId_Title",
                table: "PersonProfessionalTitles",
                columns: new[] { "PersonId", "Title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonProfessionalTitles");
        }
    }
}
