using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonHair.Migrations
{
    /// <inheritdoc />
    public partial class AddPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "Payments",
                newName: "Method");

            migrationBuilder.AddColumn<string>(
                name: "ProofImage",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofImage",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "Method",
                table: "Payments",
                newName: "PaymentMethod");
        }
    }
}
