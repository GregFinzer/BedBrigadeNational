using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BedBrigade.Data.Migrations
{
    /// <inheritdoc />
    public partial class SMSDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Phone",
                table: "Volunteers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_ContactUs_Phone",
                table: "ContactUs",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_BedRequests_Phone",
                table: "BedRequests",
                column: "Phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Volunteers_Phone",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_Users_Phone",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ContactUs_Phone",
                table: "ContactUs");

            migrationBuilder.DropIndex(
                name: "IX_BedRequests_Phone",
                table: "BedRequests");
        }
    }
}
