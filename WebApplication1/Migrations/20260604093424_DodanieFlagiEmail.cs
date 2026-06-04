using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projektBackend.Migrations
{
    /// <inheritdoc />
    public partial class DodanieFlagiEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CzyWyslanoPotwierdzenie",
                table: "Rezerwacje",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CzyWyslanoPotwierdzenie",
                table: "Rezerwacje");
        }
    }
}
