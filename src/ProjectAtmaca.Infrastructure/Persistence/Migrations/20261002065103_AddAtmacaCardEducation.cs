using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectAtmaca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAtmacaCardEducation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentlyStudying",
                table: "AtmacaCards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SchoolGrade",
                table: "AtmacaCards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolName",
                table: "AtmacaCards",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolNumber",
                table: "AtmacaCards",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrentlyStudying",
                table: "AtmacaCards");

            migrationBuilder.DropColumn(
                name: "SchoolGrade",
                table: "AtmacaCards");

            migrationBuilder.DropColumn(
                name: "SchoolName",
                table: "AtmacaCards");

            migrationBuilder.DropColumn(
                name: "SchoolNumber",
                table: "AtmacaCards");
        }
    }
}
