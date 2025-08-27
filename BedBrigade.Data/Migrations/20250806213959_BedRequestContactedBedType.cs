using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class BedRequestContactedBedType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecialInstructions",
                table: "BedRequests");

            migrationBuilder.AddColumn<string>(
                name: "BedType",
                table: "BedRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Contacted",
                table: "BedRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BedType",
                table: "BedRequests");

            migrationBuilder.DropColumn(
                name: "Contacted",
                table: "BedRequests");

            migrationBuilder.AddColumn<string>(
                name: "SpecialInstructions",
                table: "BedRequests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }
    }
}
