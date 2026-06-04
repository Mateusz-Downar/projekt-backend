using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projektBackend.Migrations
{
    /// <inheritdoc />
    public partial class DodanieCzasuTrwania : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CzasTrwaniaWGodzinach",
                table: "Rezerwacje",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CzasTrwaniaWGodzinach",
                table: "Rezerwacje");
        }
    }
}
