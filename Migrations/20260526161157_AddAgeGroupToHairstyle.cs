using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonHair.Migrations
{
    /// <inheritdoc />
    public partial class AddAgeGroupToHairstyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgeGroup",
                table: "Hairstyles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeGroup",
                table: "Hairstyles");
        }
    }
}
